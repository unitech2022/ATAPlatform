using ATA.Domain.Common;

namespace ATA.Domain.Wallet;

public static class LedgerAccounts
{
    public const string PlatformCash = "platform_cash";
    public const string GatewayClearing = "gateway_clearing";
}

public class LedgerEntry : Entity
{
    public Guid TransactionId { get; set; }
    public required string Account { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
