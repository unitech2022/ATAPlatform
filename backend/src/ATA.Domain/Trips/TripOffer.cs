using ATA.Domain.Common;

namespace ATA.Domain.Trips;

/// <summary>An offer sent to one driver for one trip; only one offer per trip is <c>sent</c> at a time.</summary>
public class TripOffer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public OfferStatus Status { get; set; } = OfferStatus.Sent;
    public decimal DriverNetEarnings { get; set; }
    public int DistanceToPickupM { get; set; }
    public int EtaSeconds { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsOpen(DateTime now) => Status == OfferStatus.Sent && now < ExpiresAt;

    public void Accept(DateTime now)
    {
        if (!IsOpen(now))
        {
            throw new DomainException(ErrorCodes.OfferExpired, new { status = Status, ExpiresAt });
        }

        Status = OfferStatus.Accepted;
        RespondedAt = now;
    }

    public void Reject(DateTime now)
    {
        if (Status != OfferStatus.Sent)
        {
            throw new DomainException(ErrorCodes.OfferExpired, new { status = Status, ExpiresAt });
        }

        Status = OfferStatus.Rejected;
        RespondedAt = now;
    }

    public void Expire(DateTime now)
    {
        Status = OfferStatus.Expired;
        RespondedAt = now;
    }
}
