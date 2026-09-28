using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Payments;

/// <summary>
/// Double-entry postings (doc 08 §F11.3): wallet movements (<c>wallet_transactions</c> + 2 entries) and wallet-less journals
/// (<c>ledger_journals</c> + 2 entries). Both are idempotent by key (a repeated key posts nothing and returns <c>null</c>).
/// The caller saves inside its transaction.
/// </summary>
public sealed class LedgerService(AtaDbContext db)
{
    public async Task<Domain.Wallet.Wallet> GetOrCreateWalletAsync(Guid userId, WalletKind kind, CancellationToken ct)
    {
        var wallet = db.Wallets.Local.FirstOrDefault(w => w.UserId == userId && w.Kind == kind)
                     ?? await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.Kind == kind, ct);
        if (wallet is null)
        {
            wallet = new Domain.Wallet.Wallet { UserId = userId, Kind = kind };
            db.Wallets.Add(wallet);
        }

        return wallet;
    }

    public async Task<WalletTransaction?> PostAsync(
        Domain.Wallet.Wallet wallet, TransactionType type, TransactionDirection direction, decimal amount, string counterpartAccount,
        string? description, string? idempotencyKey, string? referenceType, Guid? referenceId, CancellationToken ct, bool allowOverdraft = false)
    {
        if (idempotencyKey is not null && await TransactionExistsAsync(idempotencyKey, ct))
        {
            return null;
        }

        var (transaction, entries) = wallet.Post(type, direction, amount, counterpartAccount, description, idempotencyKey, referenceType, referenceId, allowOverdraft);
        db.WalletTransactions.Add(transaction);
        db.LedgerEntries.AddRange(entries);
        return transaction;
    }

    public async Task<LedgerJournal?> JournalAsync(
        JournalType type, string debitAccount, string creditAccount, decimal amount, string referenceType, Guid? referenceId,
        string? idempotencyKey, string description, CancellationToken ct, Guid? createdBy = null)
    {
        if (idempotencyKey is not null && await JournalExistsAsync(idempotencyKey, ct))
        {
            return null;
        }

        var (journal, entries) = LedgerJournal.Create(type, debitAccount, creditAccount, amount, referenceType, referenceId, idempotencyKey, description, createdBy);
        db.LedgerJournals.Add(journal);
        db.LedgerEntries.AddRange(entries);
        return journal;
    }

    public async Task<bool> TransactionExistsAsync(string key, CancellationToken ct) =>
        db.WalletTransactions.Local.Any(t => t.IdempotencyKey == key) || await db.WalletTransactions.AnyAsync(t => t.IdempotencyKey == key, ct);

    public async Task<bool> JournalExistsAsync(string key, CancellationToken ct) =>
        db.LedgerJournals.Local.Any(j => j.IdempotencyKey == key) || await db.LedgerJournals.AnyAsync(j => j.IdempotencyKey == key, ct);
}
