using System.Globalization;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

public sealed record CsvFile(string FileName, byte[] Content);

/// <summary>
/// Driver payouts (doc 08 §F11.4): the request debits the driver wallet at once (<c>payout</c>: <c>driver_wallet → payouts_pending</c>) with an IBAN
/// snapshot; rejection/cancellation reverses it (<c>payout_reversal</c>); the bank transfer posts <c>payout_paid</c>
/// (<c>payouts_pending → platform_cash</c>). Approved payouts are grouped in bank batches exported as CSV.
/// </summary>
public sealed class PayoutService(
    AtaDbContext db,
    LedgerService ledger,
    INotificationDispatcher notifications,
    ITripNotifier realtime,
    AuditService audit,
    ICurrentUser currentUser,
    IClock clock,
    IDataProtectionProvider protection,
    IOptions<PayoutsOptions> options)
{
    public const string EntityType = "payout";
    private readonly PayoutsOptions _options = options.Value;
    private readonly IDataProtector _protector = protection.CreateProtector("ATA.Payouts.Iban");

    // ----- driver -----

    public async Task<PayoutSummaryDto> SummaryAsync(CancellationToken ct)
    {
        var (driver, user) = await LoadDriverAsync(ct);
        var wallet = await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == driver.UserId && w.Kind == WalletKind.Driver, ct);
        var balance = wallet?.Balance ?? 0m;
        var pending = await db.Payouts.AsNoTracking().Where(p => p.DriverId == driver.Id && (p.Status == PayoutStatus.Requested || p.Status == PayoutStatus.Approved))
            .OrderByDescending(p => p.RequestedAt).FirstOrDefaultAsync(ct);
        var available = Math.Max(0m, balance);
        string? reason = null;
        if (string.IsNullOrWhiteSpace(driver.Iban)) reason = ErrorCodes.IbanMissing;
        else if (pending?.Status == PayoutStatus.Requested) reason = ErrorCodes.PayoutPendingExists;
        else if (balance < 0) reason = "cash_debt_outstanding";
        else if (available < _options.MinAmount) reason = "below_minimum";
        else if (driver.ApplicationStatus == ApplicationStatus.Suspended || user.Status != UserStatus.Active) reason = ErrorCodes.AccountSuspended;
        return new PayoutSummaryDto(balance, Math.Max(0m, -balance), available, _options.MinAmount, pending is null ? null : ToDto(pending),
            driver.Iban is null ? null : MaskIban(driver.Iban), reason is null, reason);
    }

    public async Task<PayoutDto> RequestAsync(PayoutRequest request, string idempotencyKey, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Amount), request.Amount)
            .Rule(nameof(request.Amount), request.Amount is null or > 0, "must be positive")
            .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
            .ThrowIfInvalid();

        var (driver, user) = await LoadDriverAsync(ct);
        var key = $"payout:{driver.Id}:{idempotencyKey}";
        var replay = await db.Payouts.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == key, ct);
        if (replay is not null)
        {
            return ToDto(replay);
        }

        if (driver.ApplicationStatus == ApplicationStatus.Suspended || user.Status != UserStatus.Active)
        {
            throw new DomainException(ErrorCodes.AccountSuspended);
        }

        if (string.IsNullOrWhiteSpace(driver.Iban))
        {
            throw new DomainException(ErrorCodes.IbanMissing);
        }

        if (await db.Payouts.AnyAsync(p => p.DriverId == driver.Id && p.Status == PayoutStatus.Requested, ct))
        {
            throw new DomainException(ErrorCodes.PayoutPendingExists);
        }

        var amount = request.Amount!.Value;
        if (amount < _options.MinAmount)
        {
            throw new DomainException(ErrorCodes.PayoutBelowMinimum, new { minAmount = _options.MinAmount });
        }

        var now = clock.UtcNow;
        var payout = await db.InTransactionAsync(async () =>
        {
            var wallet = await ledger.GetOrCreateWalletAsync(driver.UserId, WalletKind.Driver, ct);
            var created = new Payout
            {
                PayoutNumber = string.Empty,
                DriverId = driver.Id,
                WalletId = wallet.Id,
                Amount = amount,
                IbanMasked = MaskIban(driver.Iban!),
                IbanEncrypted = _protector.Protect(driver.Iban!),
                AccountHolderName = user.FullName ?? user.PhoneNumber,
                IdempotencyKey = key,
                RequestedAt = now,
            };
            if (_options.AutoApprove && amount <= _options.AutoApproveLimit)
            {
                created.Status = PayoutStatus.Approved;
                created.ApprovedAt = now;
            }

            // Wallet.Post throws insufficient_balance when the amount exceeds the balance (a cash debt leaves nothing to withdraw).
            await ledger.PostAsync(wallet, TransactionType.Payout, TransactionDirection.Debit, amount, LedgerAccounts.PayoutsPending,
                "Payout request", $"payout:{created.Id}", EntityType, created.Id, ct);
            db.Payouts.Add(created);
            created.PayoutNumber = await SequenceNumbers.NextAsync(db.Payouts.Select(p => p.PayoutNumber), $"PO-{now:yyyyMMdd}-", 5, 0, ct);
            await NotifyAsync(created, driver.UserId, ct);
            await db.SaveChangesAsync(ct);
            return created;
        }, ct);

        await realtime.PayoutRequestedAsync(new PayoutRequestedEvent(payout.Id, user.FullName, payout.Amount), ct);
        return ToDto(payout);
    }

    public async Task<PagedResult<PayoutDto>> ListOwnAsync(Paging paging, CancellationToken ct)
    {
        var (driver, _) = await LoadDriverAsync(ct);
        var query = db.Payouts.AsNoTracking().Where(p => p.DriverId == driver.Id);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(p => p.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(ToDto).ToList(), total);
    }

    public async Task<PayoutDto> CancelOwnAsync(Guid id, CancellationToken ct)
    {
        var (driver, _) = await LoadDriverAsync(ct);
        var payout = await db.Payouts.FirstOrDefaultAsync(p => p.Id == id && p.DriverId == driver.Id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        await db.InTransactionAsync(async () =>
        {
            payout.Cancel(clock.UtcNow);
            await ReverseAsync(payout, driver.UserId, ct);
            await NotifyAsync(payout, driver.UserId, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
        return ToDto(payout);
    }

    // ----- admin -----

    public async Task<PagedResult<AdminPayoutDto>> ListAsync(PayoutStatus? status, Guid? driverId, DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        var query = db.Payouts.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(p => p.Status == status);
        if (driverId is not null) query = query.Where(p => p.DriverId == driverId);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.RequestedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.RequestedAt < toAt);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(p => p.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToAdminDtosAsync(rows, ct), total);
    }

    public async Task<AdminPayoutDto> ApproveAsync(Guid id, CancellationToken ct)
    {
        var payout = Guard.NotFound(await db.Payouts.FirstOrDefaultAsync(p => p.Id == id, ct));
        var before = Snapshot(payout);
        payout.Approve(currentUser.UserId, clock.UtcNow);
        audit.Log("payout.approve", EntityType, payout.Id, before, Snapshot(payout));
        await NotifyAsync(payout, await DriverUserIdAsync(payout, ct), ct);
        await db.SaveChangesAsync(ct);
        return (await ToAdminDtosAsync([payout], ct))[0];
    }

    public async Task<AdminPayoutDto> RejectAsync(Guid id, ReasonBody request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 500).ThrowIfInvalid();
        var payout = Guard.NotFound(await db.Payouts.FirstOrDefaultAsync(p => p.Id == id, ct));
        var before = Snapshot(payout);
        var driverUserId = await DriverUserIdAsync(payout, ct);
        await db.InTransactionAsync(async () =>
        {
            payout.Reject(currentUser.UserId, request.Reason!.Trim());
            await ReverseAsync(payout, driverUserId, ct);
            audit.Log("payout.reject", EntityType, payout.Id, before, Snapshot(payout));
            await NotifyAsync(payout, driverUserId, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
        return (await ToAdminDtosAsync([payout], ct))[0];
    }

    public async Task<AdminPayoutDto> MarkPaidAsync(Guid id, MarkPaidRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.BankReference), request.BankReference, 100).ThrowIfInvalid();
        var payout = Guard.NotFound(await db.Payouts.FirstOrDefaultAsync(p => p.Id == id, ct));
        var before = Snapshot(payout);
        var driverUserId = await DriverUserIdAsync(payout, ct);
        await db.InTransactionAsync(async () =>
        {
            await PayAsync(payout, request.BankReference!.Trim(), request.PaidAt?.ToUniversalTime() ?? clock.UtcNow, driverUserId, ct);
            audit.Log("payout.mark_paid", EntityType, payout.Id, before, Snapshot(payout));
            await db.SaveChangesAsync(ct);
        }, ct);
        return (await ToAdminDtosAsync([payout], ct))[0];
    }

    public async Task<PayoutBatchDto> CreateBatchAsync(CreatePayoutBatchRequest request, CancellationToken ct)
    {
        new Validator()
            .Rule("payoutIds", request.PayoutIds is { Count: > 0 } || request.AllApproved == true, "payoutIds or allApproved=true is required")
            .ThrowIfInvalid();
        var query = db.Payouts.Where(p => p.Status == PayoutStatus.Approved && p.BatchId == null);
        if (request.PayoutIds is { Count: > 0 } ids)
        {
            query = query.Where(p => ids.Contains(p.Id));
        }

        var payouts = await query.ToListAsync(ct);
        new Validator().Rule("payoutIds", payouts.Count > 0, "no approved payouts to batch").ThrowIfInvalid();
        var now = clock.UtcNow;
        var batch = new PayoutBatch
        {
            BatchNumber = await SequenceNumbers.NextAsync(db.PayoutBatches.Select(b => b.BatchNumber), $"PB-{now:yyyyMMdd}-", 2, 0, ct),
            PayoutsCount = payouts.Count,
            TotalAmount = payouts.Sum(p => p.Amount),
            CreatedBy = currentUser.UserId,
        };
        db.PayoutBatches.Add(batch);
        foreach (var payout in payouts)
        {
            payout.BatchId = batch.Id;
        }

        audit.Log("payout_batch.create", "payout_batch", batch.Id, null, new { batch.BatchNumber, batch.PayoutsCount, batch.TotalAmount });
        await db.SaveChangesAsync(ct);
        return await BatchDtoAsync(batch, true, ct);
    }

    public async Task<PagedResult<PayoutBatchDto>> ListBatchesAsync(Paging paging, CancellationToken ct)
    {
        var total = await db.PayoutBatches.CountAsync(ct);
        var rows = await db.PayoutBatches.AsNoTracking().OrderByDescending(b => b.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = new List<PayoutBatchDto>();
        foreach (var row in rows)
        {
            items.Add(await BatchDtoAsync(row, false, ct));
        }

        return paging.Result(items, total);
    }

    public async Task<PayoutBatchDto> GetBatchAsync(Guid id, CancellationToken ct) =>
        await BatchDtoAsync(Guard.NotFound(await db.PayoutBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct)), true, ct);

    /// <summary>Bank CSV: <c>payout_number, beneficiary_name, iban, amount, currency, reference</c> (IBANs decrypted from the snapshot).</summary>
    public async Task<CsvFile> ExportBatchAsync(Guid id, CancellationToken ct)
    {
        var batch = Guard.NotFound(await db.PayoutBatches.FirstOrDefaultAsync(b => b.Id == id, ct));
        var payouts = await db.Payouts.AsNoTracking().Where(p => p.BatchId == id).OrderBy(p => p.PayoutNumber).ToListAsync(ct);
        var csv = new StringBuilder("payout_number,beneficiary_name,iban,amount,currency,reference\n");
        foreach (var p in payouts)
        {
            csv.Append(Csv.Row(p.PayoutNumber, p.AccountHolderName, _protector.Unprotect(p.IbanEncrypted), p.Amount.ToString("0.00", CultureInfo.InvariantCulture), Payment.DefaultCurrency, $"{batch.BatchNumber}/{p.PayoutNumber}"));
        }

        if (batch.Status == PayoutBatchStatus.Open)
        {
            batch.Status = PayoutBatchStatus.Exported;
        }

        batch.ExportedAt = clock.UtcNow;
        batch.ExportedBy = currentUser.UserId;
        audit.Log("payout_batch.export", "payout_batch", batch.Id, null, new { batch.BatchNumber, payouts = payouts.Count });
        await db.SaveChangesAsync(ct);
        return new CsvFile($"{batch.BatchNumber}.csv", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());
    }

    public async Task<PayoutBatchDto> MarkBatchPaidAsync(Guid id, MarkPaidRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.BankReference), request.BankReference, 100).ThrowIfInvalid();
        var batch = Guard.NotFound(await db.PayoutBatches.FirstOrDefaultAsync(b => b.Id == id, ct));
        if (batch.Status == PayoutBatchStatus.Paid)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = batch.Status });
        }

        var payouts = await db.Payouts.Where(p => p.BatchId == id && p.Status == PayoutStatus.Approved).ToListAsync(ct);
        var driverIds = payouts.Select(p => p.DriverId).Distinct().ToList();
        var driverUsers = await db.Drivers.AsNoTracking().Where(d => driverIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.UserId, ct);
        var now = request.PaidAt?.ToUniversalTime() ?? clock.UtcNow;
        await db.InTransactionAsync(async () =>
        {
            foreach (var payout in payouts)
            {
                await PayAsync(payout, request.BankReference!.Trim(), now, driverUsers[payout.DriverId], ct);
            }

            batch.Status = PayoutBatchStatus.Paid;
            batch.BankReference = request.BankReference!.Trim();
            batch.PaidAt = now;
            batch.PaidBy = currentUser.UserId;
            audit.Log("payout_batch.mark_paid", "payout_batch", batch.Id, null, new { batch.BatchNumber, payouts = payouts.Count, batch.BankReference });
            await db.SaveChangesAsync(ct);
        }, ct);
        return await BatchDtoAsync(batch, true, ct);
    }

    /// <summary>Creates an approved payout for a settlement (<c>Settlements:AutoCreatePayouts</c>); returns <c>null</c> when not possible.</summary>
    public async Task<Payout?> CreateApprovedAsync(DriverProfile driver, string holderName, decimal amount, string idempotencyKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(driver.Iban) || amount < _options.MinAmount)
        {
            return null;
        }

        var wallet = await ledger.GetOrCreateWalletAsync(driver.UserId, WalletKind.Driver, ct);
        amount = Math.Min(amount, wallet.Balance);
        if (amount < _options.MinAmount)
        {
            return null;
        }

        var now = clock.UtcNow;
        var payout = new Payout
        {
            PayoutNumber = string.Empty, DriverId = driver.Id, WalletId = wallet.Id, Amount = amount, IbanMasked = MaskIban(driver.Iban),
            IbanEncrypted = _protector.Protect(driver.Iban), AccountHolderName = holderName, IdempotencyKey = idempotencyKey, RequestedAt = now,
            Status = PayoutStatus.Approved, ApprovedAt = now, ApprovedBy = currentUser.IsAuthenticated ? currentUser.UserId : null,
        };
        await ledger.PostAsync(wallet, TransactionType.Payout, TransactionDirection.Debit, amount, LedgerAccounts.PayoutsPending, "Settlement payout", $"payout:{payout.Id}", EntityType, payout.Id, ct);
        db.Payouts.Add(payout);
        var offset = db.Payouts.Local.Count(p => p.PayoutNumber.StartsWith($"PO-{now:yyyyMMdd}-")) ;
        payout.PayoutNumber = await SequenceNumbers.NextAsync(db.Payouts.Select(p => p.PayoutNumber), $"PO-{now:yyyyMMdd}-", 5, offset, ct);
        await NotifyAsync(payout, driver.UserId, ct);
        return payout;
    }

    private async Task PayAsync(Payout payout, string bankReference, DateTime paidAt, Guid driverUserId, CancellationToken ct)
    {
        payout.MarkPaid(currentUser.UserId, bankReference, paidAt);
        await ledger.JournalAsync(JournalType.PayoutPaid, LedgerAccounts.PayoutsPending, LedgerAccounts.PlatformCash, payout.Amount, EntityType, payout.Id,
            $"payout:{payout.Id}:paid", $"Payout {payout.PayoutNumber} paid ({bankReference})", ct, currentUser.UserId);
        await NotifyAsync(payout, driverUserId, ct);
    }

    private async Task ReverseAsync(Payout payout, Guid driverUserId, CancellationToken ct)
    {
        var wallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
        await ledger.PostAsync(wallet, TransactionType.PayoutReversal, TransactionDirection.Credit, payout.Amount, LedgerAccounts.PayoutsPending,
            $"Payout {payout.PayoutNumber} reversed", $"payout:{payout.Id}:reversal", EntityType, payout.Id, ct);
    }

    private async Task NotifyAsync(Payout payout, Guid driverUserId, CancellationToken ct)
    {
        var (ar, en) = payout.Status switch
        {
            PayoutStatus.Requested => ("قيد المراجعة", "requested"),
            PayoutStatus.Approved => ("معتمد", "approved"),
            PayoutStatus.Paid => ("تم التحويل", "paid"),
            PayoutStatus.Rejected => ("مرفوض", "rejected"),
            _ => ("ملغى", "cancelled"),
        };
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.PayoutStatus, driverUserId,
            NotificationPlaceholders.Of(("payoutNumber", payout.PayoutNumber)).Money("amount", payout.Amount).Localized("status", ar, en), EntityType, payout.Id,
            new Dictionary<string, object?> { ["status"] = System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(payout.Status.ToString()) }), ct);
    }

    private async Task<Guid> DriverUserIdAsync(Payout payout, CancellationToken ct) =>
        await db.Drivers.AsNoTracking().Where(d => d.Id == payout.DriverId).Select(d => d.UserId).FirstAsync(ct);

    private async Task<(DriverProfile Driver, Domain.Identity.User User)> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        return (driver, user);
    }

    private async Task<PayoutBatchDto> BatchDtoAsync(PayoutBatch batch, bool withPayouts, CancellationToken ct)
    {
        IReadOnlyList<AdminPayoutDto>? payouts = null;
        if (withPayouts)
        {
            var rows = await db.Payouts.AsNoTracking().Where(p => p.BatchId == batch.Id).OrderBy(p => p.PayoutNumber).ToListAsync(ct);
            payouts = await ToAdminDtosAsync(rows, ct);
        }

        var ids = new[] { batch.ExportedBy, batch.PaidBy, batch.CreatedBy }.Where(i => i != null).Select(i => i!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        string? Name(Guid? id) => id is { } value ? names.GetValueOrDefault(value) : null;
        return new PayoutBatchDto(batch.Id, batch.BatchNumber, batch.Status, batch.PayoutsCount, batch.TotalAmount, batch.ExportedAt, Name(batch.ExportedBy),
            batch.BankReference, batch.PaidAt, Name(batch.PaidBy), Name(batch.CreatedBy), batch.CreatedAt, payouts);
    }

    private async Task<IReadOnlyList<AdminPayoutDto>> ToAdminDtosAsync(IReadOnlyList<Payout> rows, CancellationToken ct)
    {
        var driverIds = rows.Select(p => p.DriverId).Distinct().ToList();
        var batchIds = rows.Where(p => p.BatchId != null).Select(p => p.BatchId!.Value).Distinct().ToList();
        var drivers = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                             where driverIds.Contains(d.Id) select new { d.Id, u.FullName, u.PhoneNumber }).ToDictionaryAsync(x => x.Id, ct);
        var batches = await db.PayoutBatches.AsNoTracking().Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, b => b.BatchNumber, ct);
        return rows.Select(p => new AdminPayoutDto(p.Id, p.PayoutNumber, p.DriverId, drivers.GetValueOrDefault(p.DriverId)?.FullName, drivers.GetValueOrDefault(p.DriverId)?.PhoneNumber ?? string.Empty,
            p.Amount, p.IbanMasked, p.AccountHolderName, p.Status, p.BatchId, p.BatchId is { } b ? batches.GetValueOrDefault(b) : null, p.RequestedAt, p.ApprovedAt, p.PaidAt,
            p.RejectedReason, p.BankReference)).ToList();
    }

    public static PayoutDto ToDto(Payout p) => new(p.Id, p.PayoutNumber, p.Amount, p.IbanMasked, p.Status, p.RequestedAt, p.ApprovedAt, p.PaidAt, p.RejectedReason, p.BankReference);

    /// <summary><c>SA0380000000608010167519</c> → <c>SA03 **** **** 7519</c>.</summary>
    public static string MaskIban(string iban)
    {
        var compact = iban.Replace(" ", string.Empty).ToUpperInvariant();
        return compact.Length < 8 ? compact : $"{compact[..4]} **** **** {compact[^4..]}";
    }

    private static object Snapshot(Payout p) => new { p.Status, p.Amount, p.BankReference, p.RejectedReason, p.BatchId };
}

public static class Csv
{
    public static string Row(params string?[] values) => string.Join(',', values.Select(Escape)) + "\n";

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
