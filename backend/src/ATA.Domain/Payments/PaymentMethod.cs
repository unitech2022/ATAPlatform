using ATA.Domain.Common;

namespace ATA.Domain.Payments;

/// <summary>A saved card (<c>payment_methods</c>). Only the gateway token is stored — never a PAN or CVV (PCI DSS SAQ-A).</summary>
public class PaymentMethod : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Provider { get; set; }
    public string Type { get; set; } = "card";
    public required string GatewayToken { get; set; }
    public required string Brand { get; set; }
    public required string Last4 { get; set; }
    public byte ExpiryMonth { get; set; }
    public short ExpiryYear { get; set; }
    public string? HolderName { get; set; }
    /// <summary>Stable card fingerprint from the provider; prevents saving the same card twice.</summary>
    public string? Fingerprint { get; set; }
    public SavedCardStatus Status { get; set; } = SavedCardStatus.PendingVerification;
    public bool IsDefault { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? RemovedAt { get; set; }

    /// <summary>A card is expired once its expiry month is before the current month.</summary>
    public bool IsExpiredAt(DateTime now) => ExpiryYear < now.Year || (ExpiryYear == now.Year && ExpiryMonth < now.Month);

    public bool IsUsableAt(DateTime now) => Status == SavedCardStatus.Active && !IsExpiredAt(now);

    public void EnsureUsable(DateTime now)
    {
        if (Status != SavedCardStatus.Active)
        {
            throw new DomainException(ErrorCodes.NotFound, new { paymentMethod = Status });
        }

        if (IsExpiredAt(now))
        {
            throw new DomainException(ErrorCodes.PaymentMethodExpired, new { ExpiryMonth, ExpiryYear });
        }
    }
}
