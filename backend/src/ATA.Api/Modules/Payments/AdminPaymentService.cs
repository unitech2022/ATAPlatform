using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Payments;

/// <summary>Admin console: payments with their gateway events, refunds and ledger; wallets (adjustments, freeze); ledger account balances.</summary>
public sealed class AdminPaymentService(AtaDbContext db, RefundService refunds, LedgerService ledger, AuditService audit, ICurrentUser currentUser)
{
    public async Task<PagedResult<AdminPaymentListItemDto>> ListAsync(PaymentStatus? status, PaymentPurpose? purpose, string? provider, PaymentInstrument? method,
        DateOnly? from, DateOnly? to, string? search, Paging paging, CancellationToken ct)
    {
        var query = from p in db.Payments.AsNoTracking()
                    join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                    join t in db.Trips.AsNoTracking() on p.TripId equals t.Id into trips
                    from t in trips.DefaultIfEmpty()
                    select new { Payment = p, u.FullName, u.PhoneNumber, TripNumber = t == null ? null : t.TripNumber };
        if (status is not null) query = query.Where(x => x.Payment.Status == status);
        if (purpose is not null) query = query.Where(x => x.Payment.Purpose == purpose);
        if (!string.IsNullOrWhiteSpace(provider)) query = query.Where(x => x.Payment.Provider == provider);
        if (method is not null) query = query.Where(x => x.Payment.Method == method);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.Payment.CreatedAt >= fromAt);
        }

        if (to is { } until)
        {
            var toAt = until.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.Payment.CreatedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.PhoneNumber.Contains(phone) || (x.TripNumber != null && x.TripNumber.Contains(term))
                                     || (x.Payment.GatewayPaymentId != null && x.Payment.GatewayPaymentId.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Payment.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new AdminPaymentListItemDto(x.Payment.Id, x.Payment.Purpose, x.Payment.Status, x.Payment.Method, x.Payment.Provider,
            x.Payment.Amount, x.Payment.CapturedAmount, x.Payment.RefundedAmount, x.FullName, x.PhoneNumber, x.TripNumber, x.Payment.GatewayPaymentId, x.Payment.CreatedAt)).ToList(), total);
    }

    public async Task<AdminPaymentDetailDto> GetAsync(Guid id, CancellationToken ct)
    {
        var p = Guard.NotFound(await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct));
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == p.UserId, ct);
        var tripNumber = p.TripId is { } tripId ? await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.TripNumber).FirstOrDefaultAsync(ct) : null;
        var card = p.PaymentMethodId is { } methodId
            ? await db.PaymentMethods.AsNoTracking().Where(m => m.Id == methodId).Select(m => new PaymentCardRefDto(m.Brand, m.Last4)).FirstOrDefaultAsync(ct)
            : null;
        List<PaymentWebhookEvent> events = p.GatewayPaymentId is null
            ? []
            : await db.PaymentWebhookEvents.AsNoTracking().Where(e => e.Provider == p.Provider && e.GatewayPaymentId == p.GatewayPaymentId).OrderBy(e => e.ReceivedAt)
                .ToListAsync(ct);
        var refundRows = await db.Refunds.AsNoTracking().Where(r => r.PaymentId == p.Id).OrderBy(r => r.CreatedAt).ToListAsync(ct);
        var refundIds = refundRows.Select(r => r.Id).ToList();

        // Postings referencing the payment, its trip or its refunds (wallet movements and journals).
        var referenceIds = new List<Guid> { p.Id };
        if (p.TripId is { } t2) referenceIds.Add(t2);
        referenceIds.AddRange(refundIds);
        var transactionIds = await db.WalletTransactions.AsNoTracking().Where(t => t.ReferenceId != null && referenceIds.Contains(t.ReferenceId.Value)).Select(t => t.Id).ToListAsync(ct);
        var journalIds = await db.LedgerJournals.AsNoTracking().Where(j => j.ReferenceId != null && referenceIds.Contains(j.ReferenceId.Value)).Select(j => j.Id).ToListAsync(ct);
        var entryRows = await db.LedgerEntries.AsNoTracking()
            .Where(e => (e.TransactionId != null && transactionIds.Contains(e.TransactionId.Value)) || (e.JournalId != null && journalIds.Contains(e.JournalId.Value)))
            .OrderBy(e => e.CreatedAt).ToListAsync(ct);
        var transactions = await db.WalletTransactions.AsNoTracking().Where(t => transactionIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => new { t.Type, t.Description }, ct);
        var journals = await db.LedgerJournals.AsNoTracking().Where(j => journalIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => new { j.Type, j.Description }, ct);
        var entries = entryRows.Select(e =>
        {
            var (type, description) = e.TransactionId is { } tid && transactions.TryGetValue(tid, out var t) ? (Snake(t.Type.ToString()), t.Description)
                : e.JournalId is { } jid && journals.TryGetValue(jid, out var j) ? (Snake(j.Type.ToString()), j.Description) : (null, null);
            return new LedgerEntryDto(e.Id, e.TransactionId, e.JournalId, e.Account, e.Debit, e.Credit, type, description, e.CreatedAt);
        }).ToList();
        var eventDtos = events.Select(e => new WebhookEventDto(e.Id, e.Provider, e.EventId, e.EventType, e.GatewayPaymentId, e.SignatureValid,
            System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(e.Payload), e.ProcessingStatus, e.Error, e.ReceivedAt, e.ProcessedAt)).ToList();

        return new AdminPaymentDetailDto(p.Id, p.UserId, user.FullName, user.PhoneNumber, p.Purpose, p.Status, p.Method, p.Provider, p.Currency, p.Amount,
            p.AuthorizedAmount, p.CapturedAmount, p.RefundedAmount, p.CaptureMode, p.TripId, tripNumber, p.WalletId, p.PaymentMethodId, card, p.GatewayPaymentId,
            p.GatewayStatus, p.ActionUrl, p.ActionExpiresAt, p.FailureCode, p.FailureMessage, p.IdempotencyKey, p.AuthorizedAt, p.CapturedAt, p.FailedAt, p.VoidedAt,
            p.CreatedAt, p.UpdatedAt, p.Metadata is null ? null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(p.Metadata), eventDtos,
            await refunds.ToDtosAsync(refundRows, ct), entries);
    }

    public async Task<PagedResult<AdminWalletDto>> ListWalletsAsync(WalletKind? kind, string? search, bool? negativeOnly, Paging paging, CancellationToken ct)
    {
        var query = from w in db.Wallets.AsNoTracking()
                    join u in db.Users.AsNoTracking() on w.UserId equals u.Id
                    select new { Wallet = w, u.FullName, u.PhoneNumber };
        if (kind is not null) query = query.Where(x => x.Wallet.Kind == kind);
        if (negativeOnly == true) query = query.Where(x => x.Wallet.Balance < 0);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.PhoneNumber.Contains(phone) || (x.FullName != null && x.FullName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => (double)x.Wallet.Balance).ThenBy(x => x.Wallet.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new AdminWalletDto(x.Wallet.Id, x.Wallet.UserId, x.FullName, x.PhoneNumber, x.Wallet.Kind, x.Wallet.Balance, x.Wallet.Status)).ToList(), total);
    }

    public async Task<AdminWalletDetailDto> GetWalletAsync(Guid id, CancellationToken ct)
    {
        var wallet = Guard.NotFound(await db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct));
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == wallet.UserId, ct);
        var rows = await db.WalletTransactions.AsNoTracking().Where(t => t.WalletId == id).OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync(ct);
        var adminIds = rows.Where(t => t.ReferenceType == "admin" && t.ReferenceId != null).Select(t => t.ReferenceId!.Value).Distinct().ToList();
        var admins = await db.Users.AsNoTracking().Where(u => adminIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var transactions = rows.Select(t => new AdminWalletTransactionDto(t.Id, t.Type, t.Direction, t.Amount, t.BalanceAfter, t.Description, t.ReferenceType, t.ReferenceId,
            t.ReferenceType == "admin" && t.ReferenceId is { } adminId ? admins.GetValueOrDefault(adminId) : null, t.CreatedAt)).ToList();
        return new AdminWalletDetailDto(wallet.Id, wallet.UserId, user.FullName, user.PhoneNumber, wallet.Kind, wallet.Balance, wallet.Status, wallet.Currency,
            wallet.Debt, wallet.CreatedAt, transactions);
    }

    /// <summary>Manual adjustment against the <c>adjustments</c> account (a debit may take the balance below zero).</summary>
    public async Task<AdminWalletTransactionDto> AdjustAsync(Guid id, WalletAdjustmentRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Direction), request.Direction)
            .Require(nameof(request.Amount), request.Amount)
            .Rule(nameof(request.Amount), request.Amount is null or > 0, "must be positive")
            .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
            .Require(nameof(request.Reason), request.Reason, 255)
            .ThrowIfInvalid();
        var wallet = Guard.NotFound(await db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct));
        var before = new { wallet.Balance };
        var transaction = await db.InTransactionAsync(async () =>
        {
            var posted = (await ledger.PostAsync(wallet, TransactionType.Adjustment, request.Direction!.Value, request.Amount!.Value, LedgerAccounts.Adjustments,
                request.Reason!.Trim(), $"adjustment:{Guid.CreateVersion7()}", "admin", currentUser.UserId, ct, allowOverdraft: true))!;
            audit.Log("wallet.adjust", "wallet", wallet.Id, before, new { wallet.Balance, direction = request.Direction, amount = request.Amount, reason = request.Reason });
            await db.SaveChangesAsync(ct);
            return posted;
        }, ct);
        var adminName = await db.Users.AsNoTracking().Where(u => u.Id == currentUser.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        return new AdminWalletTransactionDto(transaction.Id, transaction.Type, transaction.Direction, transaction.Amount, transaction.BalanceAfter, transaction.Description,
            transaction.ReferenceType, transaction.ReferenceId, adminName, transaction.CreatedAt);
    }

    public async Task<AdminWalletDetailDto> SetStatusAsync(Guid id, WalletStatus status, ReasonBody? request, CancellationToken ct)
    {
        var wallet = Guard.NotFound(await db.Wallets.FirstOrDefaultAsync(w => w.Id == id, ct));
        var before = new { wallet.Status };
        if (wallet.Status == WalletStatus.Closed || wallet.Status == status)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = wallet.Status });
        }

        wallet.Status = status;
        audit.Log(status == WalletStatus.Frozen ? "wallet.freeze" : "wallet.unfreeze", "wallet", wallet.Id, before, new { wallet.Status, reason = request?.Reason?.Trim() });
        await db.SaveChangesAsync(ct);
        return await GetWalletAsync(id, ct);
    }

    private static string Snake(string name) => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(name);

    /// <summary>Debit/credit per general account for the period; wallet accounts are aggregated as <c>passenger_wallets</c> / <c>driver_wallets</c>.</summary>
    public async Task<IReadOnlyList<LedgerBalanceDto>> LedgerBalancesAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = db.LedgerEntries.AsNoTracking().AsQueryable();
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.CreatedAt < toAt);
        }

        var grouped = await query.GroupBy(e => e.Account).Select(g => new { Account = g.Key, Debit = g.Sum(e => e.Debit), Credit = g.Sum(e => e.Credit) }).ToListAsync(ct);
        return grouped
            .GroupBy(g => g.Account.StartsWith("passenger_wallet:") ? "passenger_wallets" : g.Account.StartsWith("driver_wallet:") ? "driver_wallets" : g.Account)
            .Select(g => new LedgerBalanceDto(g.Key, g.Sum(x => x.Debit), g.Sum(x => x.Credit), g.Sum(x => x.Debit) - g.Sum(x => x.Credit)))
            .OrderBy(x => x.Account, StringComparer.Ordinal)
            .ToList();
    }
}
