using ATA.Domain.Common;

namespace ATA.Domain.Wallet;

public enum WalletKind { Passenger, Driver }

public enum WalletStatus { Active, Frozen, Closed }

public enum TransactionType { Topup, TripPayment, TripEarning, Refund, Payout, Adjustment, Incentive, CancellationFee }

public enum TransactionDirection { Credit, Debit }

public class Wallet : AuditableEntity
{
    public const string DefaultCurrency = "SAR";

    public Guid UserId { get; set; }
    public WalletKind Kind { get; set; }
    public string Currency { get; set; } = DefaultCurrency;
    /// <summary>Derived value; only ever updated together with a transaction and its ledger entries.</summary>
    public decimal Balance { get; set; }
    public WalletStatus Status { get; set; } = WalletStatus.Active;

    public string LedgerAccount => $"{Kind.ToString().ToLowerInvariant()}_wallet:{Id}";

    /// <summary>Applies a movement and returns the transaction plus two balanced ledger entries.</summary>
    public (WalletTransaction Transaction, LedgerEntry[] Entries) Post(
        TransactionType type,
        TransactionDirection direction,
        decimal amount,
        string counterpartAccount,
        string? description,
        string? idempotencyKey,
        string? referenceType = null,
        Guid? referenceId = null)
    {
        if (amount <= 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { amount = "must be positive" });
        }

        if (Status != WalletStatus.Active)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }

        if (direction == TransactionDirection.Debit && Balance < amount)
        {
            throw new DomainException(ErrorCodes.InsufficientBalance, new { balance = Balance, amount });
        }

        Balance += direction == TransactionDirection.Credit ? amount : -amount;

        var transaction = new WalletTransaction
        {
            WalletId = Id,
            Type = type,
            Direction = direction,
            Amount = amount,
            BalanceAfter = Balance,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IdempotencyKey = idempotencyKey,
            Description = description,
        };

        // Wallet liability grows on credit: credit the wallet account, debit the counterpart (and vice versa).
        var walletEntry = new LedgerEntry { TransactionId = transaction.Id, Account = LedgerAccount };
        var counterEntry = new LedgerEntry { TransactionId = transaction.Id, Account = counterpartAccount };
        if (direction == TransactionDirection.Credit)
        {
            walletEntry.Credit = amount;
            counterEntry.Debit = amount;
        }
        else
        {
            walletEntry.Debit = amount;
            counterEntry.Credit = amount;
        }

        return (transaction, [counterEntry, walletEntry]);
    }
}
