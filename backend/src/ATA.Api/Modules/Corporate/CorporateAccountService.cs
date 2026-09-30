using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>The company's own account page and dashboard (<c>GET/PUT /corporate/account</c>, <c>GET /corporate/dashboard</c>).</summary>
public sealed class CorporateAccountService(
    AtaDbContext db, IClock clock, AuditService audit, CorporateExposureService exposure, CorporateTripSummaries summaries)
{
    public async Task<CorporateAccountDto> GetAsync(CorporateAccount account, CancellationToken ct) => await ToDtoAsync(account, ct);

    /// <summary>Only the billing email, address and contact can be changed by the company (legal fields are read-only); every field is required.</summary>
    public async Task<CorporateAccountDto> UpdateAsync(CorporateAccount account, UpdateCorporateAccountRequest request, string? actorRole, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.BillingEmail), request.BillingEmail, 254)
            .Rule(nameof(request.BillingEmail), request.BillingEmail is null || CorporateRules.IsEmail(request.BillingEmail.Trim()), "invalid email")
            .Require(nameof(request.ContactName), request.ContactName, 120)
            .Rule(nameof(request.ContactPhone), CorporateRules.TryNormalizePhone(request.ContactPhone, out _), "must be an E.164 phone number");
        ValidateAddress(v, request.BillingAddress, required: true);
        v.ThrowIfInvalid();

        var before = Snapshot(account);
        account.BillingEmail = request.BillingEmail!.Trim();
        account.BillingAddress = CorporateRules.SerializeAddress(CorporateRules.ToAddress(request.BillingAddress));
        account.ContactName = request.ContactName!.Trim();
        CorporateRules.TryNormalizePhone(request.ContactPhone, out var phone);
        account.ContactPhone = phone;
        audit.Log("corporate_account.update", "corporate_account", account.Id, before, Snapshot(account), CorporateMembershipService.AdminActor);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(account, ct);
    }

    /// <summary>Saudi national address rules: building 4 digits, postal code 5, additional number 4; street, district and city are text.</summary>
    public static void ValidateAddress(Validator v, NationalAddressRequest? address, bool required)
    {
        if (address is null)
        {
            v.Rule("billingAddress", !required, "required");
            return;
        }

        void Digits(string field, string? value, int length)
        {
            var text = value?.Trim();
            v.Rule($"billingAddress.{field}", string.IsNullOrEmpty(text) ? !required : text.Length == length && text.All(char.IsAsciiDigit), $"must be {length} digits");
        }

        void Text(string field, string? value) =>
            v.Rule($"billingAddress.{field}", string.IsNullOrWhiteSpace(value) ? !required : value.Trim().Length <= 120, string.IsNullOrWhiteSpace(value) ? "required" : "max_length:120");

        Digits("buildingNumber", address.BuildingNumber, 4);
        Text("street", address.Street);
        Text("district", address.District);
        Text("city", address.City);
        Digits("postalCode", address.PostalCode, 5);
        Digits("additionalNumber", address.AdditionalNumber, 4);
    }

    public async Task<CorporateAccountDto> ToDtoAsync(CorporateAccount a, CancellationToken ct)
    {
        var cityName = a.CityId is { } cityId ? await db.Cities.AsNoTracking().Where(c => c.Id == cityId).Select(c => c.NameAr).FirstOrDefaultAsync(ct) : null;
        return new CorporateAccountDto(a.Id, a.AccountNumber, a.LegalNameAr, a.LegalNameEn, a.DisplayName, a.CrNumber, a.VatNumber, a.BillingEmail, CorporateRules.ParseAddress(a.BillingAddress),
            a.CityId, cityName, a.ContactName, a.ContactPhone, a.Status, a.CreditLimit, a.BillingCycle, a.PaymentTermsDays, a.DefaultPolicyId);
    }

    public async Task<CorporateDashboardDto> DashboardAsync(CorporateAccount account, Language lang, CancellationToken ct)
    {
        var summary = await SummaryAsync(account, ct);
        var recent = await summaries.RecentAsync(account.Id, 5, lang, ct);
        return new CorporateDashboardDto(summary.MonthToDate, summary.ActiveEmployees, summary.InvitedEmployees, summary.BudgetUtilizationPercent, account.CreditLimit, summary.CreditUsed,
            summary.OpenInvoices, recent);
    }

    /// <summary>
    /// Month-to-date trips and spend (completed fares + charged corporate cancellation fees), member counts, the budget utilisation (spent ÷ budgets of the active members that have
    /// one, in percent, <c>null</c> without budgets), the credit used and the open (issued / overdue) invoices.
    /// </summary>
    public async Task<CorporateAccountSummaryDto> SummaryAsync(CorporateAccount account, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var (start, end) = CorporateRules.MonthBounds(now);
        var completed = await db.Trips.AsNoTracking()
            .Where(t => t.CorporateAccountId == account.Id && t.PaymentMethod == PaymentMethodKind.Corporate && t.Status == TripStatus.Completed && t.CompletedAt >= start && t.CompletedAt < end)
            .Select(t => t.FinalFare ?? 0m).ToListAsync(ct);
        var fees = await (from e in db.CancellationEvents.AsNoTracking()
                          join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                          where t.CorporateAccountId == account.Id && e.FeeMethod == CancellationFeeMethod.Corporate && e.FeeStatus == CancellationFeeStatus.Charged && e.CreatedAt >= start && e.CreatedAt < end
                          select e.FeeCharged).ToListAsync(ct);
        var members = await db.CorporateUsers.AsNoTracking().Where(m => m.CorporateAccountId == account.Id).ToListAsync(ct);
        var active = members.Where(m => m.Status == CorporateUserStatus.Active).ToList();
        var policies = await db.CorporatePolicies.AsNoTracking().Where(p => p.CorporateAccountId == account.Id).ToListAsync(ct);
        var spent = await exposure.SpentByEmployeeAsync(active.Select(m => m.Id).ToList(), now, ct);
        decimal budgets = 0m, budgetSpent = 0m;
        foreach (var member in active)
        {
            if (CorporateTripPolicyService.BudgetOf(member, CorporateTripPolicyService.Resolve(policies, account, member)) is { } budget && budget > 0)
            {
                budgets += budget;
                budgetSpent += spent.GetValueOrDefault(member.Id);
            }
        }

        var openInvoices = await db.CorporateInvoices.AsNoTracking()
            .Where(i => i.CorporateAccountId == account.Id && (i.Status == CorporateInvoiceStatus.Issued || i.Status == CorporateInvoiceStatus.Overdue))
            .Select(i => new { i.TotalInclVat, i.PaidAmount }).ToListAsync(ct);
        var used = await exposure.ForAccountAsync(account.Id, ct);
        return new CorporateAccountSummaryDto(
            new CorporateMonthToDateDto(completed.Count, completed.Sum() + fees.Sum()), active.Count, members.Count(m => m.Status == CorporateUserStatus.Invited),
            budgets > 0 ? decimal.Round(budgetSpent / budgets * 100m, 2) : null, used.Used,
            new CorporateOpenInvoicesDto(openInvoices.Count, openInvoices.Sum(i => Math.Max(0m, i.TotalInclVat - (i.PaidAmount ?? 0m)))));
    }

    private static object Snapshot(CorporateAccount a) => new { a.BillingEmail, a.BillingAddress, a.ContactName, a.ContactPhone };
}

/// <summary>Trip rows of the portal (dashboard and bookings list): who rides, where, how much.</summary>
public sealed class CorporateTripSummaries(AtaDbContext db)
{
    public async Task<IReadOnlyList<CorporateTripSummaryDto>> RecentAsync(Guid accountId, int count, Language lang, CancellationToken ct) =>
        await ToSummariesAsync(await db.Trips.AsNoTracking().Where(t => t.CorporateAccountId == accountId).OrderByDescending(t => t.RequestedAt).Take(count).ToListAsync(ct), lang, ct);

    public async Task<IReadOnlyList<CorporateTripSummaryDto>> ToSummariesAsync(IReadOnlyList<Trip> trips, Language lang, CancellationToken ct)
    {
        if (trips.Count == 0)
        {
            return [];
        }

        var memberIds = trips.Where(t => t.CorporateUserId != null).Select(t => t.CorporateUserId!.Value).Distinct().ToList();
        var names = await (from m in db.CorporateUsers.AsNoTracking()
                           join u in db.Users.AsNoTracking() on m.UserId equals u.Id into users
                           from u in users.DefaultIfEmpty()
                           where memberIds.Contains(m.Id)
                           select new { m.Id, Name = m.FullName ?? (u == null ? null : u.FullName) }).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var categoryIds = trips.Select(t => t.RideCategoryId).Distinct().ToList();
        var categories = await db.RideCategories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => lang.Pick(c.NameAr, c.NameEn), ct);
        return trips.Select(t => new CorporateTripSummaryDto(
            t.Id, t.TripNumber, t.Status, t.CorporateUserId is { } id ? names.GetValueOrDefault(id) : null, t.IsGuest ? t.GuestName : null, t.IsGuest, t.PickupName, t.DropoffName,
            categories.GetValueOrDefault(t.RideCategoryId), t.FinalFare ?? t.EstimatedFare, t.ScheduledAt, t.RequestedAt, t.CompletedAt)).ToList();
    }
}
