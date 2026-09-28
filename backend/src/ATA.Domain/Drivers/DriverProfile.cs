using ATA.Domain.Common;

namespace ATA.Domain.Drivers;

/// <summary>Row of the <c>drivers</c> table; carries the onboarding application state.</summary>
public class DriverProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string ApplicationNumber { get; set; }
    public ApplicationStatus ApplicationStatus { get; set; } = ApplicationStatus.Draft;
    public string? RejectionReason { get; set; }
    public string? NationalId { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Guid? CityId { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public string? Iban { get; set; }
    public DriverTier Tier { get; set; } = DriverTier.Bronze;
    public decimal RatingAvg { get; set; } = 5.00m;
    public int RatingCount { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? CurrentTripId { get; set; }
    public int AcceptanceCount { get; set; }
    public int RejectionCount { get; set; }

    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<DriverDocument> Documents { get; set; } = [];

    public bool CanEditApplication => ApplicationStatus is ApplicationStatus.Draft or ApplicationStatus.Rejected;

    public bool IsProfileComplete(string? fullName) =>
        !string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(NationalId) && DateOfBirth is not null
        && CityId is not null && Gender != Gender.Unknown;

    public void EnsureEditable()
    {
        if (!CanEditApplication)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = ApplicationStatus });
        }
    }

    public void Submit(DateTime now)
    {
        EnsureEditable();
        ApplicationStatus = ApplicationStatus.Submitted;
        RejectionReason = null;
        SubmittedAt = now;
    }

    public void StartReview()
    {
        Transition(ApplicationStatus.UnderReview, ApplicationStatus.Submitted);
    }

    public void Approve(Guid adminUserId, DateTime now)
    {
        Transition(ApplicationStatus.Approved, ApplicationStatus.Submitted, ApplicationStatus.UnderReview);
        ApprovedAt = now;
        ApprovedBy = adminUserId;
        RejectionReason = null;
    }

    public void Reject(string reason)
    {
        Transition(ApplicationStatus.Rejected, ApplicationStatus.Submitted, ApplicationStatus.UnderReview);
        RejectionReason = reason;
    }

    public void Suspend(string reason)
    {
        Transition(ApplicationStatus.Suspended, ApplicationStatus.Approved);
        RejectionReason = reason;
        IsOnline = false;
    }

    public void Reinstate()
    {
        Transition(ApplicationStatus.Approved, ApplicationStatus.Suspended);
        RejectionReason = null;
    }

    private void Transition(ApplicationStatus target, params ApplicationStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(ApplicationStatus))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = ApplicationStatus, target });
        }

        ApplicationStatus = target;
    }
}
