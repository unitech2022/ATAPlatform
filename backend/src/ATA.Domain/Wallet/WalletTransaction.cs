using ATA.Domain.Common;

namespace ATA.Domain.Wallet;

public class WalletTransaction : Entity
{
    public Guid WalletId { get; set; }
    public TransactionType Type { get; set; }
    public TransactionDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? Description { get; set; }
}
