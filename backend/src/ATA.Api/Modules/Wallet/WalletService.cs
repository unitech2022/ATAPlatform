using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Passengers;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Wallet;

public sealed class PaymentsOptions
{
    public const string Section = "Payments";
    public bool SandboxEnabled { get; set; }
    public decimal MinTopup { get; set; } = 10m;
    public decimal MaxTopup { get; set; } = 5000m;
}

/// <summary>Wallet read model, transaction history and sandbox top-ups written atomically with balanced ledger entries.</summary>
public sealed class WalletService(AtaDbContext db, ICurrentUser currentUser, IOptions<PaymentsOptions> options)
{
    private readonly PaymentsOptions _payments = options.Value;

    public async Task<WalletDto> GetAsync(WalletKind? requestedKind, Language lang, CancellationToken ct)
    {
        var kind = ResolveKind(requestedKind);
        var wallet = await GetOrCreateAsync(kind, ct);
        await db.SaveChangesAsync(ct);

        var defaultMethod = PaymentMethodKind.Wallet;
        if (kind == WalletKind.Passenger)
        {
            var userId = currentUser.UserId;
            defaultMethod = await db.Passengers.Where(p => p.UserId == userId).Select(p => p.DefaultPaymentMethod).FirstOrDefaultAsync(ct);
        }

        var methods = new List<PaymentMethodDto>
        {
            new("wallet", lang.Pick("محفظة ATA", "ATA Wallet"), defaultMethod == PaymentMethodKind.Wallet),
            new("cash", lang.Pick("الدفع نقداً", "Cash"), defaultMethod == PaymentMethodKind.Cash),
        };
        return new WalletDto(wallet.Id, wallet.Kind, wallet.Currency, wallet.Balance, methods);
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

    public async Task<TopupResponse> TopupAsync(WalletKind? requestedKind, TopupRequest request, string idempotencyKey, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Amount), request.Amount)
            .Rule(nameof(request.Amount), request.Amount is null || (request.Amount >= _payments.MinTopup && request.Amount <= _payments.MaxTopup), $"must be between {_payments.MinTopup} and {_payments.MaxTopup}")
            .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
            .Rule(nameof(request.Method), request.Method == "sandbox" && _payments.SandboxEnabled, "only 'sandbox' is accepted while Payments:SandboxEnabled=true")
            .ThrowIfInvalid();

        var kind = ResolveKind(requestedKind);
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var wallet = await GetOrCreateAsync(kind, ct);
            await db.SaveChangesAsync(ct);

            var existing = await db.WalletTransactions.AsNoTracking().FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null)
            {
                if (existing.WalletId != wallet.Id)
                {
                    throw new DomainException(ErrorCodes.Conflict, new { idempotencyKey = "already used for another wallet" });
                }

                await tx.CommitAsync(ct);
                return new TopupResponse(existing.Id, existing.BalanceAfter);
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
            await tx.CommitAsync(ct);
            return new TopupResponse(transaction.Id, wallet.Balance);
        });
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
