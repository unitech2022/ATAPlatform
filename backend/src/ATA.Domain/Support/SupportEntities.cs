using ATA.Domain.Common;

namespace ATA.Domain.Support;

public enum HelpAudience { Passenger, Driver, All }

public enum SupportTicketType { TripIssue, PaymentIssue, LostItem, Safety, Account, Other }

public enum SupportRequesterRole { Passenger, Driver, CorporateAdmin }

public enum SupportTicketStatus { Open, PendingUser, InProgress, Resolved, Closed }

public enum SupportPriority { Urgent, High, Normal, Low }

public enum SupportChannel { App, Website, Dashboard, Phone }

/// <summary>Who wrote a message (and <c>last_message_by</c>): the requester, an agent or the system.</summary>
public enum SupportAuthorRole { User, Agent, System }

public enum DisputeReason { Overcharged, RouteLonger, WaitingCharged, CancellationFee, PromoNotApplied, Other }

public enum DisputeStatus { Open, UnderReview, Approved, PartiallyApproved, Rejected }

public enum DisputeResolution { RefundFull, RefundPartial, NoRefund }

/// <summary>Queue colouring of a ticket (doc 11 §F18.3): <c>due_soon</c> = within 30 minutes of a deadline.</summary>
public enum SlaState { Ok, DueSoon, Breached }

/// <summary>Row of <c>help_categories</c> (doc 11 §F18.1).</summary>
public class HelpCategory : AuditableEntity
{
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public required string Icon { get; set; }
    public HelpAudience Audience { get; set; } = HelpAudience.All;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Row of <c>help_articles</c>: bilingual Markdown, published or draft, with view and helpfulness counters.</summary>
public class HelpArticle : AuditableEntity
{
    public Guid CategoryId { get; set; }
    public required string Slug { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public HelpAudience Audience { get; set; } = HelpAudience.All;
    /// <summary>JSON array of strings.</summary>
    public string? Tags { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public int HelpfulYes { get; set; }
    public int HelpfulNo { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>Row of <c>support_sla_policies</c>: one per priority.</summary>
public class SupportSlaPolicy
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public SupportPriority Priority { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// A support ticket (<c>support_tickets</c>, doc 11 §F18.1). The SLA clock is paused while the ticket waits for the user
/// (<see cref="SlaPausedAt"/>); the time spent paused is added to <see cref="ResolutionDueAt"/> when the user answers.
/// </summary>
public class SupportTicket : AuditableEntity
{
    public static readonly TimeSpan DueSoonWindow = TimeSpan.FromMinutes(30);

    public required string TicketNumber { get; set; }
    public Guid RequesterUserId { get; set; }
    public SupportRequesterRole RequesterRole { get; set; }
    public SupportTicketType Type { get; set; }
    public Guid? TripId { get; set; }
    public required string Subject { get; set; }
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public SupportPriority Priority { get; set; } = SupportPriority.Normal;
    public SupportChannel Channel { get; set; } = SupportChannel.App;
    public Guid? AssignedToUserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime FirstResponseDueAt { get; set; }
    public DateTime ResolutionDueAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? SlaPausedAt { get; set; }
    public int SlaPausedSeconds { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public SupportAuthorRole LastMessageBy { get; set; } = SupportAuthorRole.User;
    public int UnreadByUser { get; set; }
    public byte? CsatScore { get; set; }
    public string? CsatComment { get; set; }
    /// <summary>Plain columns (no FK): the linked safety case / lost item report reference the ticket back.</summary>
    public Guid? SafetyCaseId { get; set; }
    public Guid? LostItemReportId { get; set; }

    /// <summary>Not <c>resolved</c> and not <c>closed</c>: the ticket still counts in the queue and against the SLA.</summary>
    public bool IsActive => Status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed);

    public bool IsClosed => Status == SupportTicketStatus.Closed;

    public void EnsureNotClosed()
    {
        if (IsClosed)
        {
            throw new DomainException(ErrorCodes.TicketClosed);
        }
    }

    /// <summary>Sets both due dates from the priority's policy (creation and priority changes): <c>created + minutes (+ paused seconds for the resolution)</c>.</summary>
    public void ApplySla(SupportSlaPolicy policy)
    {
        FirstResponseDueAt = CreatedAt.AddMinutes(policy.FirstResponseMinutes);
        ResolutionDueAt = CreatedAt.AddMinutes(policy.ResolutionMinutes).AddSeconds(SlaPausedSeconds);
    }

    public void PauseSla(DateTime now) => SlaPausedAt ??= now;

    /// <summary>Adds the time spent paused to <see cref="SlaPausedSeconds"/> and pushes <see cref="ResolutionDueAt"/> back by the same amount.</summary>
    public void ResumeSla(DateTime now)
    {
        if (SlaPausedAt is not { } pausedAt)
        {
            return;
        }

        var seconds = (int)Math.Max(0, (now - pausedAt).TotalSeconds);
        SlaPausedSeconds += seconds;
        ResolutionDueAt = ResolutionDueAt.AddSeconds(seconds);
        SlaPausedAt = null;
    }

    /// <summary>The resolution deadline right now: while paused it moves with the clock.</summary>
    public DateTime EffectiveResolutionDueAt(DateTime now) => SlaPausedAt is { } pausedAt ? ResolutionDueAt.Add(now - pausedAt) : ResolutionDueAt;

    public bool IsFirstResponseBreached(DateTime now) => IsActive && FirstResponseAt is null && now > FirstResponseDueAt;

    public bool IsResolutionBreached(DateTime now) => IsActive && now > EffectiveResolutionDueAt(now);

    public SlaState SlaStateAt(DateTime now)
    {
        if (!IsActive)
        {
            return SlaState.Ok;
        }

        if (IsFirstResponseBreached(now) || IsResolutionBreached(now))
        {
            return SlaState.Breached;
        }

        var firstSoon = FirstResponseAt is null && FirstResponseDueAt - now <= DueSoonWindow;
        var resolutionSoon = EffectiveResolutionDueAt(now) - now <= DueSoonWindow;
        return firstSoon || resolutionSoon ? SlaState.DueSoon : SlaState.Ok;
    }

    /// <summary>Priority a new ticket of <paramref name="type"/> starts with (doc 11 §F18.2).</summary>
    public static SupportPriority DefaultPriority(SupportTicketType type) => type switch
    {
        SupportTicketType.Safety => SupportPriority.Urgent,
        SupportTicketType.PaymentIssue => SupportPriority.High,
        _ => SupportPriority.Normal,
    };

    /// <summary>Types that must reference a trip the user took part in.</summary>
    public static bool RequiresTrip(SupportTicketType type) => type is SupportTicketType.TripIssue or SupportTicketType.PaymentIssue or SupportTicketType.LostItem;

    public static int PriorityRank(SupportPriority priority) => priority switch
    {
        SupportPriority.Urgent => 0,
        SupportPriority.High => 1,
        SupportPriority.Normal => 2,
        _ => 3,
    };
}

/// <summary>Row of <c>support_messages</c>: a public message of the user / agent, an internal agent note or a system line.</summary>
public class SupportMessage : Entity
{
    public Guid TicketId { get; set; }
    public Guid? AuthorUserId { get; set; }
    public SupportAuthorRole AuthorRole { get; set; }
    public required string Body { get; set; }
    public bool IsInternal { get; set; }
}

/// <summary>Row of <c>support_message_attachments</c>: a stored file attached to a message.</summary>
public class SupportMessageAttachment : Entity
{
    public Guid MessageId { get; set; }
    public Guid FileId { get; set; }
}

/// <summary>Row of <c>canned_responses</c>; bodies may use <c>{userName}</c>, <c>{ticketNumber}</c> and <c>{tripNumber}</c>.</summary>
public class CannedResponse : AuditableEntity
{
    public required string Code { get; set; }
    public required string Title { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public SupportTicketType? TicketType { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
}

/// <summary>Row of <c>fare_disputes</c>: at most one per trip, resolved by an agent with the <c>support.disputes</c> permission.</summary>
public class FareDispute : AuditableEntity
{
    public Guid TicketId { get; set; }
    public Guid TripId { get; set; }
    public Guid RequesterUserId { get; set; }
    public DisputeReason Reason { get; set; }
    /// <summary>What the passenger paid (the final fare) or the cancellation fee charged.</summary>
    public decimal ChargedAmount { get; set; }
    public decimal? RequestedRefundAmount { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public DisputeResolution? Resolution { get; set; }
    public decimal? ApprovedRefundAmount { get; set; }
    /// <summary>The F11 refund created by the resolution (plain column: <c>refunds.dispute_id</c> points back).</summary>
    public Guid? RefundId { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }

    public bool IsResolved => Status is DisputeStatus.Approved or DisputeStatus.PartiallyApproved or DisputeStatus.Rejected;
}

/// <summary>Math shared by the API and the tests (medians of the KPIs).</summary>
public static class SupportMath
{
    /// <summary>Median of the values (mean of the two middle ones for an even count); null without values.</summary>
    public static double? Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        var sorted = values.OrderBy(v => v).ToArray();
        var middle = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2d;
    }
}
