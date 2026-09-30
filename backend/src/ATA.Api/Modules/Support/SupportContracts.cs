using ATA.Api.Modules.Payments;
using ATA.Domain.Safety;
using ATA.Domain.Support;

namespace ATA.Api.Modules.Support;

/// <summary><c>Support:*</c> settings (doc 11 §F18.4).</summary>
public sealed class SupportOptions
{
    public const string Section = "Support";
    /// <summary>A <c>resolved</c> ticket without a user reply is closed by <c>SupportAutoCloseJob</c> after this many days.</summary>
    public int AutoCloseDays { get; set; } = 3;
    /// <summary>A fare can be disputed this many days after the trip completed (or was cancelled with a fee).</summary>
    public int DisputeWindowDays { get; set; } = 14;
    public int MaxAttachmentsPerMessage { get; set; } = 5;
    /// <summary>Runs <c>SupportAutoCloseJob</c> and <c>SupportSlaMonitorJob</c> (<c>false</c> in tests, which call <c>RunOnceAsync</c>).</summary>
    public bool JobsEnabled { get; set; } = true;
    public int AutoCloseIntervalMinutes { get; set; } = 60;
    public int SlaMonitorIntervalMinutes { get; set; } = 5;
}

// ----- public help center -----

public sealed record HelpCategoryDto(Guid Id, string Code, string Name, string Icon, int ArticlesCount);

public sealed record HelpArticleSummaryDto(Guid Id, string Slug, string Title, string Excerpt, Guid CategoryId, DateTime UpdatedAt);

public sealed record HelpArticleCategoryDto(Guid Id, string Code, string Name);

public sealed record HelpRelatedDto(string Slug, string Title);

public sealed record HelpArticleDto(Guid Id, string Slug, string Title, string Body, HelpArticleCategoryDto Category, IReadOnlyList<string>? Tags, DateTime UpdatedAt,
    IReadOnlyList<HelpRelatedDto> Related);

public sealed record HelpFeedbackRequest(bool? Helpful);

// ----- admin help center -----

public sealed record AdminHelpCategoryDto(Guid Id, string Code, string NameAr, string NameEn, string Icon, HelpAudience Audience, int SortOrder, bool IsActive,
    int ArticlesCount, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record HelpCategoryUpsertRequest(string? Code, string? NameAr, string? NameEn, string? Icon, HelpAudience? Audience, int? SortOrder, bool? IsActive);

public sealed record AdminHelpArticleDto(Guid Id, string Slug, Guid CategoryId, string? CategoryName, string TitleAr, string TitleEn, string BodyAr, string BodyEn, HelpAudience Audience,
    IReadOnlyList<string> Tags, int SortOrder, bool IsPublished, DateTime? PublishedAt, int ViewCount, int HelpfulYes, int HelpfulNo, Guid? UpdatedBy, string? UpdatedByName,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record HelpArticleUpsertRequest(Guid? CategoryId, string? Slug, string? TitleAr, string? TitleEn, string? BodyAr, string? BodyEn, HelpAudience? Audience,
    List<string>? Tags, int? SortOrder, bool? IsPublished);

// ----- rider / driver support -----

public sealed record AttachmentUploadDto(Guid FileId, string FileName, string ContentType, long SizeBytes);

public sealed record DisputeRequest(DisputeReason? Reason, decimal? RequestedRefundAmount);

/// <summary>Optional extras of a <c>lost_item</c> ticket (the report's category defaults to <c>other</c> and the phone to the rider's number).</summary>
public sealed record LostItemTicketRequest(LostItemCategory? ItemCategory, string? ContactPhone);

public sealed record CreateTicketRequest(SupportTicketType? Type, Guid? TripId, string? Subject, string? Message, List<Guid>? FileIds, DisputeRequest? Dispute, LostItemTicketRequest? LostItem);

public sealed record TicketSummaryDto(Guid Id, string TicketNumber, SupportTicketType Type, string Subject, SupportTicketStatus Status, string? TripNumber, DateTime LastMessageAt,
    int Unread, DateTime CreatedAt);

public sealed record TicketAttachmentDto(Guid FileId, string FileName, string ContentType);

public sealed record TicketMessageDto(Guid Id, SupportAuthorRole AuthorRole, string? AuthorName, string Body, IReadOnlyList<TicketAttachmentDto> Attachments, DateTime CreatedAt);

public sealed record TicketTripDto(Guid Id, string TripNumber, DateTime? CompletedAt);

public sealed record TicketDisputeDto(DisputeReason Reason, decimal ChargedAmount, decimal? RequestedRefundAmount, DisputeStatus Status, DisputeResolution? Resolution, decimal? ApprovedRefundAmount);

public sealed record TicketDetailDto(Guid Id, string TicketNumber, SupportTicketType Type, string Subject, SupportTicketStatus Status, SupportPriority Priority, TicketTripDto? Trip,
    IReadOnlyList<TicketMessageDto> Messages, TicketDisputeDto? Dispute, bool CanReply, bool CanRate, int? CsatScore, DateTime CreatedAt, DateTime? ResolvedAt);

public sealed record ReplyRequest(string? Body, List<Guid>? FileIds);

public sealed record CsatRequest(int? Score, string? Comment);

// ----- admin -----

public sealed record SupportSummaryDto(int Open, int Unassigned, int PendingUser, int BreachingFirstResponse, int BreachingResolution, double? AvgFirstResponseMinutes,
    double? AvgResolutionHours, double? CsatAvg);

/// <summary>Row of <c>GET /admin/support/tickets</c> and payload of the <c>SupportTicketCreated</c> hub event.</summary>
public sealed record AdminTicketListItemDto(Guid Id, string TicketNumber, SupportTicketType Type, string Subject, SupportTicketStatus Status, SupportPriority Priority,
    string? RequesterName, SupportRequesterRole RequesterRole, string? TripNumber, string? AssignedToName, DateTime FirstResponseDueAt, DateTime ResolutionDueAt, SlaState SlaState,
    DateTime LastMessageAt, SupportAuthorRole LastMessageBy, DateTime CreatedAt, SupportChannel Channel, DateTime? FirstResponseAt, Guid RequesterUserId);

public sealed record AdminTicketMessageDto(Guid Id, SupportAuthorRole AuthorRole, Guid? AuthorUserId, string? AuthorName, string Body, bool IsInternal,
    IReadOnlyList<TicketAttachmentDto> Attachments, DateTime CreatedAt);

public sealed record AdminTicketRequesterDto(Guid UserId, string? FullName, string PhoneNumber, SupportRequesterRole Role, string Language, DateTime CreatedAt, Guid? DriverId);

public sealed record AdminTicketTripDto(Guid Id, string TripNumber, string Status, string? PickupName, string? DropoffName, decimal? FinalFare, decimal EstimatedFare, string PaymentMethod,
    DateTime RequestedAt, DateTime? CompletedAt, DateTime? CancelledAt, string? DriverName, ReceiptDto? Receipt);

public sealed record AdminLinkedCaseDto(Guid Id, string Number, string Status);

public sealed record AdminTicketLinksDto(AdminLinkedCaseDto? SafetyCase, AdminLinkedCaseDto? LostItemReport);

public sealed record AdminTicketDetailDto(Guid Id, string TicketNumber, SupportTicketType Type, string Subject, SupportTicketStatus Status, SupportPriority Priority,
    SupportChannel Channel, Guid? AssignedToUserId, string? AssignedToName, DateTime? AssignedAt, DateTime FirstResponseDueAt, DateTime ResolutionDueAt, DateTime? FirstResponseAt,
    DateTime? SlaPausedAt, int SlaPausedSeconds, SlaState SlaState, DateTime? ResolvedAt, DateTime? ClosedAt, DateTime LastMessageAt, SupportAuthorRole LastMessageBy, int UnreadByUser,
    int? CsatScore, string? CsatComment, DateTime CreatedAt, AdminTicketRequesterDto Requester, AdminTicketTripDto? Trip, IReadOnlyList<AdminTicketMessageDto> Messages,
    AdminDisputeDto? Dispute, AdminTicketLinksDto Linked, Guid? SafetyCaseId, Guid? LostItemReportId, DateTime UpdatedAt);

public sealed record AdminCreateTicketRequest(Guid? RequesterUserId, SupportTicketType? Type, Guid? TripId, string? Subject, string? Message, SupportPriority? Priority,
    SupportChannel? Channel);

public sealed record AdminMessageRequest(string? Body, List<Guid>? FileIds, bool? IsInternal, string? CannedResponseCode);

public sealed record AssignTicketRequest(Guid? UserId);

public sealed record TicketStatusRequest(SupportTicketStatus? Status, string? Note);

public sealed record TicketPriorityRequest(SupportPriority? Priority);

public sealed record TicketTypeRequest(SupportTicketType? Type);

public sealed record ResolveDisputeRequest(DisputeResolution? Resolution, decimal? Amount, string? Note);

/// <summary>A fare dispute for agents; <c>refund</c> is the F11 refund created by the resolution (status <c>pending_approval</c> above the four-eyes limit).</summary>
public sealed record AdminDisputeDto(Guid Id, Guid TicketId, string? TicketNumber, Guid TripId, string? TripNumber, Guid RequesterUserId, string? RequesterName, DisputeReason Reason,
    decimal ChargedAmount, decimal? RequestedRefundAmount, DisputeStatus Status, DisputeResolution? Resolution, decimal? ApprovedRefundAmount, Guid? RefundId, Guid? ResolvedBy,
    string? ResolvedByName, DateTime? ResolvedAt, string? ResolutionNote, DateTime CreatedAt, RefundDto? Refund);

public sealed record CannedResponseDto(Guid Id, string Code, string Title, string BodyAr, string BodyEn, SupportTicketType? TicketType, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CannedResponseUpsertRequest(string? Code, string? Title, string? BodyAr, string? BodyEn, SupportTicketType? TicketType, bool? IsActive);

public sealed record SlaPolicyDto(SupportPriority Priority, int FirstResponseMinutes, int ResolutionMinutes, DateTime UpdatedAt);

public sealed record SlaPolicyUpsertRequest(SupportPriority? Priority, int? FirstResponseMinutes, int? ResolutionMinutes);

public sealed record SupportTypeStatsDto(SupportTicketType Type, int Created, int Resolved);

/// <summary>KPIs of doc 11 §F18.5 over a period (no range = all time). Resolution time is <c>resolved_at − created_at − sla_paused_seconds</c>.</summary>
public sealed record SupportStatsDto(int Created, int Resolved, int Closed, int OpenNow, double? AvgResolutionMinutes, double? MedianResolutionMinutes, double? AvgFirstResponseMinutes,
    double? MedianFirstResponseMinutes, double? SlaCompliance, double? CsatAvg, int CsatCount, IReadOnlyList<SupportTypeStatsDto> ByType);

/// <summary>SignalR <c>SupportTicketUpdated</c> for the ticket's requester.</summary>
public sealed record SupportTicketUserEvent(Guid TicketId, SupportTicketStatus Status, DateTime LastMessageAt, int Unread);

/// <summary>SignalR <c>SupportTicketUpdated</c> for the <c>admins</c> group (<c>slaState</c> is only set by <c>SupportSlaMonitorJob</c>).</summary>
public sealed record SupportTicketAdminEvent(Guid TicketId, SupportTicketStatus Status, SupportPriority Priority, SupportAuthorRole LastMessageBy, SlaState? SlaState = null);
