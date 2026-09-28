using System.Text.Json;
using ATA.Api.Modules.Trips;
using ATA.Domain.Safety;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Safety;

/// <summary><c>Safety:*</c> settings (doc 09 §F12.9).</summary>
public sealed class SafetyOptions
{
    public const string Section = "Safety";
    public string ShareBaseUrl { get; set; } = "https://ata.sa";
    public int ShareExpiryMinutesAfterEnd { get; set; } = 30;
    public int MaxSharesPerTrip { get; set; } = 10;
    public bool AutoShareSmsEnabled { get; set; } = true;
    public int PublicShareRatePerMinute { get; set; } = 30;
    /// <summary>Polling interval returned to the public tracking page.</summary>
    public int PublicShareRefreshSeconds { get; set; } = 10;
    public string EmergencyNumber { get; set; } = "911";
    public int SosDedupMinutes { get; set; } = 10;
    public int SosLocationIntervalSeconds { get; set; } = 10;
    public string[] OpsHotlinePhones { get; set; } = [];
    public bool ChatMaskPhoneNumbers { get; set; } = true;
    public int MonitorIntervalSeconds { get; set; } = 30;
    public double StopSpeedMps { get; set; } = 1.5;
    public int StopMinutes { get; set; } = 5;
    public int StopIgnoreRadiusMeters { get; set; } = 300;
    public int DeviationMeters { get; set; } = 2000;
    public int DeviationMetersMaps { get; set; } = 500;
    public int DeviationSeconds { get; set; } = 120;
    public double OverrunFactor { get; set; } = 2.0;
    public int OverrunMinMinutes { get; set; } = 15;
    public int AlertCooldownMinutes { get; set; } = 10;
    public int CheckResponseSeconds { get; set; } = 120;
    public int ReportWindowDays { get; set; } = 7;
    public int LostItemWindowDays { get; set; } = 7;
    /// <summary><c>Retention:TripMessagesDays</c>: chat messages older than this are purged by <c>TripShareExpiryJob</c>.</summary>
    public int TripMessagesRetentionDays { get; set; } = 180;
    /// <summary>Runs <c>SafetyMonitorJob</c>, <c>SafetyCheckTimeoutJob</c> and <c>TripShareExpiryJob</c> (<c>false</c> in tests).</summary>
    public bool JobsEnabled { get; set; } = true;
}

public sealed class CallMaskingOptions
{
    public const string Section = "CallMasking";
    /// <summary><c>none</c> (default): calls are unavailable and the app falls back to the in-app chat.</summary>
    public string Provider { get; set; } = "none";
}

// ----- trusted contacts & shares -----

public sealed record TrustedContactRequest(string? Name, string? PhoneNumber, string? Relationship, bool? AutoShare, bool? NotifyOnSos);

public sealed record TrustedContactDto(Guid Id, string Name, string PhoneNumber, string? Relationship, bool AutoShare, bool NotifyOnSos, DateTime CreatedAt);

public sealed record CreateShareRequest(TripShareChannel? Channel, List<Guid>? ContactIds);

public sealed record CreatedShareDto(Guid Id, string Url, TripShareChannel Channel, Guid? TrustedContactId, DateTime? ExpiresAt);

public sealed record CreateSharesResponse(IReadOnlyList<CreatedShareDto> Shares);

public sealed record TripShareDto(Guid Id, string Url, TripShareChannel Channel, Guid? TrustedContactId, string? TrustedContactName, int ViewCount, DateTime? ExpiresAt, DateTime? RevokedAt, DateTime? LastViewedAt, DateTime CreatedAt);

public sealed record PublicDriverDto(string FirstName, decimal RatingAvg, string? PhotoUrl);

public sealed record PublicCategoryDto(string Code, string Name);

public sealed record PublicPlaceDto(string Name, string? Address, decimal Lat, decimal Lng);

public sealed record PublicDriverLocationDto(decimal Lat, decimal Lng, decimal? Heading, DateTime UpdatedAt);

public sealed record PublicRouteDto(IReadOnlyList<decimal[]> Planned, IReadOnlyList<decimal[]> Travelled);

public sealed record PublicTimelineDto(DateTime? AssignedAt, DateTime? ArrivedAt, DateTime? StartedAt, DateTime? CompletedAt, DateTime? CancelledAt);

/// <summary><c>GET /public/trip-shares/{token}</c>: never exposes phone numbers, family names, the PIN, the fare or the payment method.</summary>
public sealed record PublicTripShareDto(
    string Status,
    string? PassengerFirstName,
    PublicDriverDto? Driver,
    TripVehicleDto? Vehicle,
    PublicCategoryDto? RideCategory,
    PublicPlaceDto Pickup,
    PublicPlaceDto Dropoff,
    IReadOnlyList<PublicPlaceDto> Stops,
    PublicDriverLocationDto? DriverLocation,
    PublicRouteDto Route,
    int? EtaSeconds,
    string? EtaTarget,
    PublicTimelineDto Timeline,
    DateTime? ExpiresAt,
    int RefreshSeconds);

// ----- SOS, reports, cases, alerts (rider / driver) -----

public sealed record SosRequest(Guid? TripId, string? Role, decimal? Lat, decimal? Lng, decimal? Accuracy, string? Note, bool? NotifyTrustedContacts);

public sealed record SosResponse(Guid CaseId, string CaseNumber, SafetyCaseStatus Status, string EmergencyNumber, int ContactsNotified);

public sealed record SosLocationRequest(decimal? Lat, decimal? Lng, decimal? Accuracy);

public sealed record SosCancelRequest(string? Reason);

public sealed record SafetyReportRequest(Guid? TripId, SafetyReportCategory? Category, string? Description, List<Guid>? FileIds);

public sealed record PublicNoteDto(string Body, DateTime CreatedAt);

public sealed record SafetyCaseSummaryDto(Guid Id, string CaseNumber, SafetyCaseType Type, SafetyCaseStatus Status, SafetyPriority Priority, Guid? TripId, string? TripNumber, DateTime OpenedAt, DateTime? ResolvedAt, IReadOnlyList<PublicNoteDto> PublicNotes);

public sealed record SafetyAlertDto(Guid Id, Guid TripId, SafetyAlertType Type, SafetyAlertStatus Status, DateTime DetectedAt, DateTime? RespondBy);

public sealed record AlertRespondRequest(SafetyAlertResponse? Response, decimal? Lat, decimal? Lng);

// ----- chat & calls -----

public sealed record TripMessageDto(Guid Id, Guid TripId, TripMessageSender SenderRole, TripMessageKind Kind, string Body, string? QuickReplyCode, bool IsMine, DateTime? ReadAt, DateTime CreatedAt);

public sealed record AdminTripMessageDto(Guid Id, Guid TripId, TripMessageSender SenderRole, string? SenderName, TripMessageKind Kind, string Body, string? QuickReplyCode, DateTime? ReadAt, DateTime CreatedAt);

public sealed record SendMessageRequest(string? Body, string? QuickReplyCode);

public sealed record MarkMessagesReadRequest(Guid? UpToId);

public sealed record TripMessagesReadEvent(Guid TripId, Guid UpToId);

/// <summary><c>mode</c> = <c>proxy</c> | <c>unavailable</c>; <c>available</c> mirrors it for clients that read a boolean. Never the real number.</summary>
public sealed record MaskedCallDto(string Mode, bool Available, string? ProxyNumber, string? Pin, DateTime? ExpiresAt);

public sealed record QuickReplyDto(string Code, string Text);

// ----- lost items -----

public sealed record LostItemRequest(LostItemCategory? ItemCategory, string? Description, string? ContactPhone);

public sealed record LostItemDto(Guid Id, string ReportNumber, Guid TripId, string? TripNumber, LostItemCategory ItemCategory, string Description, string? ContactPhone,
    LostItemStatus Status, LostItemDriverResponse? DriverResponse, string? DriverNote, Guid? SupportTicketId, DateTime CreatedAt);

public sealed record DriverLostItemDto(Guid Id, string ReportNumber, string? TripNumber, LostItemCategory ItemCategory, string Description, LostItemStatus Status,
    LostItemDriverResponse? DriverResponse, DateTime CreatedAt);

public sealed record LostItemRespondRequest(bool? Found, string? Note);

public sealed record AdminLostItemDto(
    Guid Id, string ReportNumber, Guid TripId, string? TripNumber, Guid ReporterUserId, string? ReporterName, string? ReporterPhone, string? ContactPhone,
    Guid? DriverId, string? DriverName, LostItemCategory ItemCategory, string Description, LostItemStatus Status, LostItemDriverResponse? DriverResponse,
    string? DriverNote, DateTime? DriverRespondedAt, Guid? SupportTicketId, DateTime? ClosedAt, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record LostItemUpdateRequest(LostItemStatus? Status, string? Note);

// ----- admin -----

public sealed record OpenCasesDto(int Critical, int High, int Medium, int Low);

public sealed record SafetySummaryDto(OpenCasesDto Open, int Unassigned, int? AvgFirstResponseSeconds, int PendingAlerts, int OnDutyAgents);

/// <summary>Row of <c>GET /admin/safety/cases</c> and payload of <c>SafetyCaseOpened</c> / <c>SafetyCaseUpdated</c>.</summary>
public sealed record AdminSafetyCaseListItemDto(
    Guid Id, string CaseNumber, SafetyCaseType Type, SafetyCaseSource Source, SafetyPriority Priority, SafetyCaseStatus Status, Guid? TripId, string? TripNumber,
    string? ReporterName, SafetyReporterRole ReporterRole, string? AssignedToName, DateTime OpenedAt, DateTime? FirstResponseAt, int AgeSeconds, decimal? LastLat, decimal? LastLng);

public sealed record SafetyCaseNoteDto(Guid Id, SafetyNoteKind Kind, string Body, bool IsInternal, Guid? AuthorUserId, string? AuthorName, DateTime CreatedAt);

public sealed record SafetyCaseAttachmentDto(Guid Id, Guid FileId, string? FileName, string? UploadedByName, DateTime CreatedAt);

public sealed record SafetyPartyDto(Guid Id, Guid UserId, string? FullName, string PhoneNumber);

public sealed record SafetyCaseTripDto(Guid Id, string TripNumber, TripStatus Status, PlaceDto Pickup, PlaceDto Dropoff, IReadOnlyList<PlaceDto> Stops, IReadOnlyList<decimal[]> PlannedRoute,
    TripRideCategoryDto? RideCategory, SafetyPartyDto? Passenger, SafetyPartyDto? Driver, TripVehicleDto? Vehicle, DateTime? StartedAt);

public sealed record SafetyLiveLocationDto(decimal Lat, decimal Lng, decimal? Heading, DateTime? UpdatedAt, string Source);

public sealed record SafetyAlertMetricsDto(int? StoppedSeconds = null, int? DeviationMeters = null, int? DeviationSeconds = null, int? ElapsedSeconds = null, int? EstimatedSeconds = null);

/// <summary>Row of <c>GET /admin/safety/alerts</c> and payload of <c>SafetyAlertRaised</c>.</summary>
public sealed record AdminSafetyAlertDto(
    Guid Id, Guid TripId, string? TripNumber, SafetyAlertType Type, SafetyAlertStatus Status, DateTime DetectedAt, decimal? Lat, decimal? Lng, SafetyAlertMetricsDto? Metrics,
    DateTime? PromptedAt, DateTime? RespondBy, DateTime? RespondedAt, SafetyAlertResponse? Response, Guid? SafetyCaseId, string? SafetyCaseNumber, string? DismissedByName, DateTime CreatedAt);

public sealed record AdminSafetyCaseDetailDto(
    Guid Id, string CaseNumber, SafetyCaseType Type, SafetyCaseSource Source, SafetyPriority Priority, SafetyCaseStatus Status, Guid? TripId, string? TripNumber,
    string? ReporterName, SafetyReporterRole ReporterRole, string? AssignedToName, DateTime OpenedAt, DateTime? FirstResponseAt, int AgeSeconds,
    Guid? ReporterUserId, string? ReporterPhone, Guid? SubjectUserId, string? SubjectName, SafetyReportCategory? ReportCategory, string? Description,
    decimal? Lat, decimal? Lng, decimal? LastLat, decimal? LastLng, DateTime? LastLocationAt, int ContactsNotified, Guid? AssignedToUserId, DateTime? AssignedAt,
    SafetyEscalationTarget? EscalatedTo, SafetyResolutionCode? ResolutionCode, string? Resolution, DateTime? ReporterCancelledAt, Guid? SupportTicketId, DateTime? ResolvedAt,
    SafetyCaseTripDto? Trip, SafetyLiveLocationDto? LiveLocation, IReadOnlyList<AdminSafetyAlertDto> Alerts, IReadOnlyList<SafetyCaseNoteDto> Notes,
    IReadOnlyList<SafetyCaseAttachmentDto> Attachments, int SharesCount, int TrustedContactsNotified);

public sealed record AssignCaseRequest(Guid? UserId);

public sealed record CaseStatusRequest(SafetyCaseStatus? Status, SafetyEscalationTarget? EscalatedTo, string? Note);

public sealed record CaseNoteRequest(string? Body, SafetyNoteKind? Kind, bool? IsInternal);

public sealed record ResolveCaseRequest(SafetyResolutionCode? ResolutionCode, string? Resolution);

public sealed record AdminCreateCaseRequest(Guid? TripId, SafetyCaseType? Type, SafetyPriority? Priority, string? Description, Guid? SubjectUserId);

public sealed record DismissAlertRequest(string? Note);

/// <summary>SignalR <c>SafetyCheck</c> payload (passenger).</summary>
public sealed record SafetyCheckEvent(Guid AlertId, Guid TripId, SafetyAlertType Type, DateTime? RespondBy);

public static class SafetyJson
{
    public static SafetyAlertMetricsDto? Metrics(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SafetyAlertMetricsDto>(json, Common.JsonDefaults.Options);
}
