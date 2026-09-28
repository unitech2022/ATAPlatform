using ATA.Domain.Common;

namespace ATA.Domain.Safety;

/// <summary>A public tracking link of a trip (<c>trip_shares</c>); the token is 128 random bits in base64url (22 characters).</summary>
public class TripShare : Entity
{
    public Guid TripId { get; set; }
    public required string Token { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? TrustedContactId { get; set; }
    public TripShareChannel Channel { get; set; }
    /// <summary>Set when the trip ends (end + <c>Safety:ShareExpiryMinutesAfterEnd</c>).</summary>
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }

    public bool IsUsableAt(DateTime now) => RevokedAt is null && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>A person the user trusts (<c>trusted_contacts</c>), at most 5 per user.</summary>
public class TrustedContact : AuditableEntity
{
    public const int MaxPerUser = 5;

    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public required string PhoneNumber { get; set; }
    public string? Relationship { get; set; }
    public bool AutoShare { get; set; }
    public bool NotifyOnSos { get; set; } = true;
}

/// <summary>A safety case handled by operations (<c>safety_cases</c>): SOS, escalated automatic alerts and safety reports.</summary>
public class SafetyCase : AuditableEntity
{
    public required string CaseNumber { get; set; }
    public SafetyCaseType Type { get; set; }
    public SafetyCaseSource Source { get; set; }
    public SafetyPriority Priority { get; set; }
    public SafetyCaseStatus Status { get; set; } = SafetyCaseStatus.Open;
    public Guid? TripId { get; set; }
    public Guid? ReporterUserId { get; set; }
    public SafetyReporterRole ReporterRole { get; set; }
    /// <summary>The reported party (the other side of the trip for a safety report).</summary>
    public Guid? SubjectUserId { get; set; }
    public SafetyReportCategory? ReportCategory { get; set; }
    public string? Description { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public decimal? LastLat { get; set; }
    public decimal? LastLng { get; set; }
    public DateTime? LastLocationAt { get; set; }
    public byte ContactsNotified { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public SafetyEscalationTarget? EscalatedTo { get; set; }
    public SafetyResolutionCode? ResolutionCode { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ReporterCancelledAt { get; set; }
    /// <summary>F18 support ticket (plain column until F18).</summary>
    public Guid? SupportTicketId { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public bool IsOpen => Status != SafetyCaseStatus.Resolved;

    public void EnsureOpen()
    {
        if (!IsOpen)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }
    }

    /// <summary>The first administrative action (assignment, note, status change, resolution) stamps <see cref="FirstResponseAt"/> once.</summary>
    public void MarkResponded(DateTime now) => FirstResponseAt ??= now;
}

public class SafetyCaseNote : Entity
{
    public Guid CaseId { get; set; }
    /// <summary><c>null</c> = written by the system.</summary>
    public Guid? AuthorUserId { get; set; }
    public SafetyNoteKind Kind { get; set; }
    public required string Body { get; set; }
    public bool IsInternal { get; set; } = true;
}

public class SafetyCaseAttachment : Entity
{
    public Guid CaseId { get; set; }
    public Guid FileId { get; set; }
    public Guid UploadedBy { get; set; }
}

/// <summary>An automatic anomaly detected during a trip (<c>safety_alerts</c>) and the "are you OK?" cycle.</summary>
public class SafetyAlert : Entity
{
    public Guid TripId { get; set; }
    public SafetyAlertType Type { get; set; }
    public SafetyAlertStatus Status { get; set; } = SafetyAlertStatus.PendingRider;
    public DateTime DetectedAt { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    /// <summary>JSON <c>{ stoppedSeconds, deviationMeters, deviationSeconds, elapsedSeconds, estimatedSeconds }</c>.</summary>
    public string? Metrics { get; set; }
    public DateTime? PromptedAt { get; set; }
    public DateTime? RespondBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public SafetyAlertResponse? Response { get; set; }
    public Guid? SafetyCaseId { get; set; }
    public Guid? DismissedBy { get; set; }
    /// <summary>When the alert left <c>pending_rider</c> (drives the per-type cooldown).</summary>
    public DateTime? ClosedAt { get; set; }

    public bool IsPending => Status == SafetyAlertStatus.PendingRider;

    public void Close(SafetyAlertStatus status, DateTime now)
    {
        if (!IsPending)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }

        Status = status;
        ClosedAt = now;
    }
}

/// <summary>In-trip chat between passenger and driver (<c>trip_messages</c>).</summary>
public class TripMessage : Entity
{
    public const int MaxBodyLength = 500;

    public Guid TripId { get; set; }
    public Guid? SenderUserId { get; set; }
    public TripMessageSender SenderRole { get; set; }
    public TripMessageKind Kind { get; set; }
    public required string Body { get; set; }
    public string? QuickReplyCode { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>A passenger's lost item report (<c>lost_item_reports</c>).</summary>
public class LostItemReport : AuditableEntity
{
    public required string ReportNumber { get; set; }
    public Guid TripId { get; set; }
    public Guid ReporterUserId { get; set; }
    public Guid? DriverId { get; set; }
    public LostItemCategory ItemCategory { get; set; }
    public required string Description { get; set; }
    public string? ContactPhone { get; set; }
    public LostItemStatus Status { get; set; } = LostItemStatus.Open;
    public LostItemDriverResponse? DriverResponse { get; set; }
    public string? DriverNote { get; set; }
    public DateTime? DriverRespondedAt { get; set; }
    /// <summary>F18 support ticket (plain column until F18).</summary>
    public Guid? SupportTicketId { get; set; }
    public Guid? ClosedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
}
