using ATA.Domain.Common;

namespace ATA.Domain.Drivers;

public class DriverDocument : AuditableEntity
{
    public Guid DriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public Guid FileId { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public DateOnly? ExpiresAt { get; set; }
    public string? ReviewNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public void Review(DocumentStatus status, string? note, Guid reviewerUserId, DateTime now)
    {
        if (status is not (DocumentStatus.Verified or DocumentStatus.Rejected))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { status = "must be verified or rejected" });
        }

        Status = status;
        ReviewNote = note;
        ReviewedBy = reviewerUserId;
        ReviewedAt = now;
    }
}
