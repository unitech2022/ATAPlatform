using ATA.Api.Common;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Wallet;

/// <summary>
/// Wallet read model, transaction history and top-ups: <c>sandbox</c> (no gateway, kept while <c>Payments:SandboxEnabled=true</c>) or
/// <c>card</c> / <c>apple_pay</c> through the payment gateway (<see cref="PaymentService"/>), all written with balanced ledger entries.
/// </summary>
public sealed class WalletService(AtaDbContext db, ICurrentUser currentUser, PaymentService payments, IClock clock, IOptions<PaymentsOptions> options, IOptions<PayoutsOptions> payouts)
{
    private readonly PaymentsOptions _payments = options.Value;

    public async Task<WalletDto> GetAsync(WalletKind? requestedKind, Language lang, CancellationToken ct)
    {
        var kind = ResolveKind(requestedKind);
        var wallet = await GetOrCreateAsync(kind, ct);
        await db.SaveChangesAsync(ct);

        var defaultMethod = PaymentMethodKind.Wallet;
        Guid? defaultCard = null;
        var methods = new List<PaymentMethodDto>();
        if (kind == WalletKind.Passenger)
        {
            var userId = currentUser.UserId;
            var passenger = await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => new { p.DefaultPaymentMethod, p.DefaultPaymentMethodId }).FirstOrDefaultAsync(ct);
            defaultMethod = passenger?.DefaultPaymentMethod ?? PaymentMethodKind.Cash;
            defaultCard = passenger?.DefaultPaymentMethodId;
        }

        methods.Add(new("wallet", lang.Pick("محفظة ATA", "ATA Wallet"), defaultMethod == PaymentMethodKind.Wallet));
        methods.Add(new("cash", lang.Pick("الدفع نقداً", "Cash"), defaultMethod == PaymentMethodKind.Cash));
        if (kind == WalletKind.Passenger)
        {
            var now = clock.UtcNow;
            var userId = currentUser.UserId;
            var cards = await db.PaymentMethods.AsNoTracking().Where(m => m.UserId == userId && m.Status == SavedCardStatus.Active).OrderByDescending(m => m.IsDefault).ToListAsync(ct);
            methods.AddRange(cards.Where(c => !c.IsExpiredAt(now)).Select(c => new PaymentMethodDto("card", PaymentMethodService.Label(c, lang),
                defaultMethod == PaymentMethodKind.Card && c.Id == defaultCard, c.Id, c.Brand, c.Last4)));
        }

        var driver = kind == WalletKind.Driver;
        return new WalletDto(wallet.Id, wallet.Kind, wallet.Currency, wallet.Balance, methods, driver ? wallet.Debt : null, wallet.Status,
            driver ? payouts.Value.MaxCashDebt : null);
    }

    public async Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(WalletKind? requestedKind, Paging paging, CancellationToken ct)
    {
        var wallet = await GetOrCreateAsync(ResolveKind(requestedKind), ct);
        await db.SaveChangesAsync(ct);
        var query = db.WalletTransactions.AsNoTracking().Where(t => t.WalletId == wallet.Id);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(t => t.CreatedAt).Skip(paging.Skip).Take(paging.PageSize)
            .Select(t => new WalletTransactionDto(t.Id, t.Type, t.Direction, t.Amount, t.BalanceAfter, t.Description, t.CreatedAt))
            .ToListAsync(ct);
        return paging.Result(items, total);
    }

    public async Task<TopupOutcome> TopupAsync(WalletKind? requestedKind, TopupRequest request, string idempotencyKey, CancellationToken ct)
    {
        var method = request.Method;
        new Validator()
            .Require(nameof(request.Amount), request.Amount)
            .Rule(nameof(request.Amount), request.Amount is null || (request.Amount >= _payments.MinTopup && request.Amount <= _payments.MaxTopup), $"must be between {_payments.MinTopup} and {_payments.MaxTopup}")
            .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
            .Rule(nameof(request.Method), method is "card" or "apple_pay" || (method == "sandbox" && _payments.SandboxEnabled),
                _payments.SandboxEnabled ? "must be sandbox|card|apple_pay" : "must be card|apple_pay")
            .ThrowIfInvalid();

        var kind = ResolveKind(requestedKind);
        if (method != "sandbox")
        {
            var cardWallet = await GetOrCreateAsync(kind, ct);
            await db.SaveChangesAsync(ct);
            return await payments.TopupAsync(cardWallet, request, idempotencyKey, ct);
        }

        return await db.InTransactionAsync(async () =>
        {
            var wallet = await GetOrCreateAsync(kind, ct);
            await db.SaveChangesAsync(ct);

            var existing = await db.WalletTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null)
            {
                if (existing.WalletId != wallet.Id)
                {
                    throw new DomainException(ErrorCodes.Conflict, new { idempotencyKey = "already used for another wallet" });
                }

                return new TopupOutcome(false, new TopupResponse(existing.Id, existing.BalanceAfter, null, PaymentStatus.Captured, null));
            }

            var (transaction, entries) = wallet.Post(
                TransactionType.Topup,
                TransactionDirection.Credit,
                request.Amount!.Value,
                LedgerAccounts.GatewayClearing,
                "Sandbox top-up",
                idempotencyKey,
                referenceType: "sandbox");
            db.WalletTransactions.Add(transaction);
            db.LedgerEntries.AddRange(entries);
            await db.SaveChangesAsync(ct);
            return new TopupOutcome(false, new TopupResponse(transaction.Id, wallet.Balance, null, PaymentStatus.Captured, null));
        }, ct);
    }

    private WalletKind ResolveKind(WalletKind? requested)
    {
        var hasPassenger = currentUser.HasRole(RoleNames.Passenger);
        var hasDriver = currentUser.HasRole(RoleNames.Driver);
        if (requested is null)
        {
            if (hasPassenger) return WalletKind.Passenger;
            if (hasDriver) return WalletKind.Driver;
            throw new DomainException(ErrorCodes.Forbidden);
        }

        var allowed = requested == WalletKind.Passenger ? hasPassenger : hasDriver;
        return allowed ? requested.Value : throw new DomainException(ErrorCodes.Forbidden, new { kind = requested });
    }

    private async Task<Domain.Wallet.Wallet> GetOrCreateAsync(WalletKind kind, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.Kind == kind, ct);
        if (wallet is null)
        {
            wallet = new Domain.Wallet.Wallet { UserId = userId, Kind = kind };
            db.Wallets.Add(wallet);
        }

        return wallet;
    }
}
