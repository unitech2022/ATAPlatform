using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// Platform-side management of corporate accounts (doc 12 §F19.4 "الإدارة", permission <c>corporate.manage</c>): create (<c>pending</c>), edit, activate (needs one active company
/// admin), suspend / close with a reason, receivables. Every write is audited as <c>corporate_account.*</c> with <c>entity_id</c> = the account.
/// </summary>
public sealed class CorporateAdminService(
    AtaDbContext db, ICurrentUser currentUser, AuditService audit, CorporateAccountService accounts, CorporateExposureService exposure)
{
    public const string EntityType = "corporate_account";

    public async Task<PagedResult<AdminCorporateAccountListItemDto>> ListAsync(CorporateAccountStatus? status, Guid? cityId, string? search, Paging paging, CancellationToken ct)
    {
        var query = from a in db.CorporateAccounts.AsNoTracking()
                    join c in db.Cities.AsNoTracking() on a.CityId equals c.Id into cities
                    from c in cities.DefaultIfEmpty()
                    select new { Account = a, CityName = c == null ? null : c.NameAr };
        if (status is not null) query = query.Where(x => x.Account.Status == status);
        if (cityId is { } city) query = query.Where(x => x.Account.CityId == city);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Account.DisplayName.Contains(term) || x.Account.LegalNameAr.Contains(term) || x.Account.LegalNameEn.Contains(term) || x.Account.AccountNumber.Contains(term) || x.Account.CrNumber.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Account.CreatedAt).ThenBy(x => x.Account.AccountNumber).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new AdminCorporateAccountListItemDto(x.Account.Id, x.Account.AccountNumber, x.Account.DisplayName, x.Account.LegalNameAr, x.Account.LegalNameEn,
            x.Account.CrNumber, x.Account.Status, x.Account.CityId, x.CityName, x.Account.CreditLimit, x.Account.CreatedAt)).ToList(), total);
    }

    public async Task<AdminCorporateAccountDto> GetAsync(Guid id, CancellationToken ct)
    {
        var account = Guard.NotFound(await db.CorporateAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct));
        var summary = await accounts.SummaryAsync(account, ct);
        return await ToDtoAsync(account, summary, ct);
    }

    public async Task<AdminCorporateAccountDto> CreateAsync(CorporateAccountInput input, CancellationToken ct)
    {
        await ValidateAsync(input, null, ct);
        var account = new CorporateAccount
        {
            AccountNumber = string.Empty, LegalNameAr = input.LegalNameAr!.Trim(), LegalNameEn = input.LegalNameEn!.Trim(), DisplayName = input.DisplayName!.Trim(), CrNumber = input.CrNumber!.Trim(),
            BillingEmail = input.BillingEmail!.Trim(), ContactName = input.ContactName!.Trim(), ContactPhone = string.Empty, Status = CorporateAccountStatus.Pending, CreatedBy = currentUser.UserId,
        };
        Apply(account, input);
        db.CorporateAccounts.Add(account);
        audit.Log("corporate_account.create", EntityType, account.Id, null, Snapshot(account));
        for (var attempt = 0; ; attempt++)
        {
            account.AccountNumber = await SequenceNumbers.NextAsync(db.CorporateAccounts.Select(a => a.AccountNumber), "CA-", 5, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
                // Another account took the same number: retry with the next one.
            }
        }

        return await ToDtoAsync(account, null, ct);
    }

    public async Task<AdminCorporateAccountDto> UpdateAsync(Guid id, CorporateAccountInput input, CancellationToken ct)
    {
        var account = Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == id, ct));
        await ValidateAsync(input, id, ct);
        var before = Snapshot(account);
        account.LegalNameAr = input.LegalNameAr!.Trim();
        account.LegalNameEn = input.LegalNameEn!.Trim();
        account.DisplayName = input.DisplayName!.Trim();
        account.CrNumber = input.CrNumber!.Trim();
        account.BillingEmail = input.BillingEmail!.Trim();
        account.ContactName = input.ContactName!.Trim();
        Apply(account, input);
        audit.Log("corporate_account.update", EntityType, account.Id, before, Snapshot(account));
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(account, null, ct);
    }

    /// <summary><c>pending|suspended → active</c>, only with at least one active company admin (<c>409 conflict { reason: no_active_admin }</c>).</summary>
    public async Task<AdminCorporateAccountDto> ActivateAsync(Guid id, CancellationToken ct)
    {
        var account = Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == id, ct));
        if (account.Status is not (CorporateAccountStatus.Pending or CorporateAccountStatus.Suspended))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invalid_status", status = account.Status });
        }

        if (!await db.CorporateUsers.AnyAsync(m => m.CorporateAccountId == id && m.Role == CorporateRole.CorporateAdmin && m.Status == CorporateUserStatus.Active, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "no_active_admin" });
        }

        var before = new { status = account.Status };
        account.Status = CorporateAccountStatus.Active;
        audit.Log("corporate_account.activate", EntityType, id, before, new { status = account.Status });
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(account, null, ct);
    }

    public async Task<AdminCorporateAccountDto> SuspendAsync(Guid id, string? reason, CancellationToken ct)
    {
        new Validator().Require(nameof(reason), reason, 500).ThrowIfInvalid();
        var account = Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == id, ct));
        if (account.Status is not (CorporateAccountStatus.Active or CorporateAccountStatus.Pending))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invalid_status", status = account.Status });
        }

        var before = new { status = account.Status };
        account.Status = CorporateAccountStatus.Suspended;
        audit.Log("corporate_account.suspend", EntityType, id, before, new { status = account.Status, reason = reason!.Trim() });
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(account, null, ct);
    }

    public async Task<AdminCorporateAccountDto> CloseAsync(Guid id, string? reason, CancellationToken ct)
    {
        new Validator().Require(nameof(reason), reason, 500).ThrowIfInvalid();
        var account = Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == id, ct));
        if (account.Status == CorporateAccountStatus.Closed)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invalid_status", status = account.Status });
        }

        var before = new { status = account.Status };
        account.Status = CorporateAccountStatus.Closed;
        audit.Log("corporate_account.close", EntityType, id, before, new { status = account.Status, reason = reason!.Trim() });
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(account, null, ct);
    }

    /// <summary><c>GET /admin/corporate/receivables</c>: per company the credit limit, the unbilled amount, the unpaid invoices and the overdue part.</summary>
    public async Task<IReadOnlyList<CorporateReceivableDto>> ReceivablesAsync(CancellationToken ct)
    {
        var result = new List<CorporateReceivableDto>();
        foreach (var a in await db.CorporateAccounts.AsNoTracking().OrderBy(a => a.AccountNumber).ToListAsync(ct))
        {
            var e = await exposure.ForAccountAsync(a.Id, ct);
            result.Add(new CorporateReceivableDto(a.Id, a.DisplayName, a.CreditLimit, e.Unbilled, e.UnpaidInvoices, e.OverdueAmount));
        }

        return result;
    }

    public async Task<CorporateAccount> RequireAccountAsync(Guid id, CancellationToken ct) =>
        Guard.NotFound(await db.CorporateAccounts.FirstOrDefaultAsync(a => a.Id == id, ct));

    // ----- helpers -----

    private async Task ValidateAsync(CorporateAccountInput input, Guid? existingId, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(input.LegalNameAr), input.LegalNameAr, 200)
            .Require(nameof(input.LegalNameEn), input.LegalNameEn, 200)
            .Require(nameof(input.DisplayName), input.DisplayName, 120)
            .Require(nameof(input.CrNumber), input.CrNumber)
            .Rule(nameof(input.CrNumber), input.CrNumber is null || CorporateRules.IsCrNumber(input.CrNumber.Trim()), "must be 10 digits")
            .Rule(nameof(input.VatNumber), string.IsNullOrWhiteSpace(input.VatNumber) || CorporateRules.IsVatNumber(input.VatNumber.Trim()), "must be 15 digits starting and ending with 3")
            .Require(nameof(input.BillingEmail), input.BillingEmail, 254)
            .Rule(nameof(input.BillingEmail), input.BillingEmail is null || CorporateRules.IsEmail(input.BillingEmail.Trim()), "invalid email")
            .Require(nameof(input.ContactName), input.ContactName, 120)
            .Rule(nameof(input.ContactPhone), CorporateRules.TryNormalizePhone(input.ContactPhone, out _), "must be an E.164 phone number")
            .Rule(nameof(input.CreditLimit), input.CreditLimit is null or >= 0, "must be positive")
            .Rule(nameof(input.PaymentTermsDays), input.PaymentTermsDays is null or (>= 0 and <= 365), "must be between 0 and 365")
            .Rule(nameof(input.Notes), input.Notes is null || input.Notes.Length <= 1000, "max_length:1000");
        CorporateAccountService.ValidateAddress(v, input.BillingAddress, required: false);
        if (input.CityId is { } cityId)
        {
            v.Rule(nameof(input.CityId), await db.Cities.AnyAsync(c => c.Id == cityId, ct), "unknown city");
        }

        v.ThrowIfInvalid();
        var cr = input.CrNumber!.Trim();
        if (await db.CorporateAccounts.AnyAsync(a => a.CrNumber == cr && a.Id != existingId, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "cr_number_exists" });
        }
    }

    private static void Apply(CorporateAccount account, CorporateAccountInput input)
    {
        account.VatNumber = CorporateRules.Clean(input.VatNumber);
        account.BillingAddress = CorporateRules.SerializeAddress(CorporateRules.ToAddress(input.BillingAddress));
        account.CityId = input.CityId;
        CorporateRules.TryNormalizePhone(input.ContactPhone, out var phone);
        account.ContactPhone = phone;
        account.CreditLimit = input.CreditLimit ?? account.CreditLimit;
        account.BillingCycle = input.BillingCycle ?? BillingCycle.Monthly;
        account.PaymentTermsDays = input.PaymentTermsDays ?? account.PaymentTermsDays;
        account.Notes = CorporateRules.Clean(input.Notes);
    }

    private async Task<AdminCorporateAccountDto> ToDtoAsync(CorporateAccount a, CorporateAccountSummaryDto? summary, CancellationToken ct)
    {
        var cityName = a.CityId is { } cityId ? await db.Cities.AsNoTracking().Where(c => c.Id == cityId).Select(c => c.NameAr).FirstOrDefaultAsync(ct) : null;
        return new AdminCorporateAccountDto(a.Id, a.AccountNumber, a.DisplayName, a.LegalNameAr, a.LegalNameEn, a.CrNumber, a.Status, a.CityId, cityName, a.CreditLimit, a.CreatedAt, a.VatNumber,
            a.BillingEmail, CorporateRules.ParseAddress(a.BillingAddress), a.ContactName, a.ContactPhone, a.BillingCycle, a.PaymentTermsDays, a.DefaultPolicyId, a.Notes, a.UpdatedAt, summary);
    }

    private static object Snapshot(CorporateAccount a) => new
    {
        a.AccountNumber, a.LegalNameAr, a.LegalNameEn, a.DisplayName, a.CrNumber, a.VatNumber, a.BillingEmail, a.CityId, a.ContactName, a.ContactPhone, a.Status, a.CreditLimit, a.PaymentTermsDays,
    };
}
