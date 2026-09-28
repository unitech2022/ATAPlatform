using ATA.Domain.Common;

namespace ATA.Domain.Wallet;

/// <summary>Ledger account names (see README "Ledger accounts" and doc 08 §F11.3 — the single reference for every feature).</summary>
public static class LedgerAccounts
{
    /// <summary>Platform bank/cash account: payouts leave it, corporate payments enter it (asset).</summary>
    public const string PlatformCash = "platform_cash";
    /// <summary>Card money held by the gateway until it is settled to the bank (asset).</summary>
    public const string GatewayClearing = "gateway_clearing";
    /// <summary>Non-cash trip fares; the driver share is paid out of it, the remainder is the platform commission (revenue).</summary>
    public const string TripRevenue = "trip_revenue";
    /// <summary>Cash trips: driver share against the cash the driver collected; the net credit is the cash commission.</summary>
    public const string CashCollected = "cash_collected";
    /// <summary>Payouts requested by drivers and not transferred yet (liability).</summary>
    public const string PayoutsPending = "payouts_pending";
    /// <summary>Amounts refunded to passengers (contra revenue).</summary>
    public const string Refunds = "refunds";
    /// <summary>Cancellation fees (F14); driver compensation is paid out of it (revenue).</summary>
    public const string CancellationFees = "cancellation_fees";
    public const string DiscountPromotion = "discount_promotion";
    public const string DiscountFavoriteDriver = "discount_favorite_driver";
    public const string Incentives = "incentives";
    public const string Adjustments = "adjustments";

    public static string CorporateReceivable(Guid corporateAccountId) => $"corporate_receivable:{corporateAccountId}";
}

/// <summary>One side of a balanced posting. Exactly one of <see cref="TransactionId"/> (wallet movement) or <see cref="JournalId"/> (journal) is set.</summary>
public class LedgerEntry : Entity
{
    public Guid? TransactionId { get; set; }
    public Guid? JournalId { get; set; }
    public required string Account { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

/// <summary>Journal types for postings that do not touch a wallet (<c>ledger_journals.type</c>).</summary>
public enum JournalType
{
    TripCardCapture,
    TripDiscount,
    TripCorporateCharge,
    CancellationFeeCard,
    RefundCard,
    PayoutPaid,
    CorporateInvoicePayment,
    Manual,
}

/// <summary>A balanced group of ledger entries that does not involve a wallet (card captures, refunds to cards, payouts paid…).</summary>
public class LedgerJournal : Entity
{
    public JournalType Type { get; set; }
    public required string ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public required string Description { get; set; }
    public Guid? CreatedBy { get; set; }

    /// <summary>Builds a two-line journal: debit <paramref name="debitAccount"/>, credit <paramref name="creditAccount"/>.</summary>
    public static (LedgerJournal Journal, LedgerEntry[] Entries) Create(
        JournalType type, string debitAccount, string creditAccount, decimal amount, string referenceType, Guid? referenceId,
        string? idempotencyKey, string description, Guid? createdBy = null)
    {
        if (amount <= 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { amount = "must be positive" });
        }

        var journal = new LedgerJournal
        {
            Type = type, ReferenceType = referenceType, ReferenceId = referenceId, IdempotencyKey = idempotencyKey, Description = description, CreatedBy = createdBy,
        };
        return (journal,
        [
            new LedgerEntry { JournalId = journal.Id, Account = debitAccount, Debit = amount },
            new LedgerEntry { JournalId = journal.Id, Account = creditAccount, Credit = amount },
        ]);
    }
}
