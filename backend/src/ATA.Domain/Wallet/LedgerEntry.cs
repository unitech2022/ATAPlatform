using ATA.Domain.Common;

namespace ATA.Domain.Wallet;

public static class LedgerAccounts
{
    public const string PlatformCash = "platform_cash";
    public const string GatewayClearing = "gateway_clearing";
    /// <summary>Trip fares collected through wallets; the driver share is paid out of it, the remainder is the platform commission.</summary>
    public const string TripRevenue = "trip_revenue";
    /// <summary>Cash fares collected by drivers on the platform's behalf (asset held by drivers until settlement).</summary>
    public const string CashCollected = "cash_collected";
}

public class LedgerEntry : Entity
{
    public Guid TransactionId { get; set; }
    public required string Account { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
