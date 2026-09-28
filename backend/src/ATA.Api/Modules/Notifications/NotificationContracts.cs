using System.Text.Json;
using ATA.Domain.Notifications;

namespace ATA.Api.Modules.Notifications;

public sealed class NotificationsOptions
{
    public const string Section = "Notifications";
    /// <summary>Runs <see cref="NotificationDeliveryWorker"/> (<c>false</c> in tests, which call the processor directly).</summary>
    public bool WorkerEnabled { get; set; } = true;
    /// <summary>Runs the campaign sender and the document-expiry scanner (<c>false</c> in tests).</summary>
    public bool JobsEnabled { get; set; } = true;
    public int WorkerPollSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int PushBatchSize { get; set; } = 2000;
    public int CampaignPushPerMinute { get; set; } = 60;
    public int CampaignPollSeconds { get; set; } = 30;
    public int[] DocumentExpiryOffsetsDays { get; set; } = [30, 7, 1];
    public int DocumentScanHourLocal { get; set; } = 6;
}

public sealed record NotificationDto(Guid Id, string Type, NotificationCategory Category, string Title, string Body, JsonElement? Data, DateTime? ReadAt, DateTime CreatedAt);

public sealed record NotificationsPage(IReadOnlyList<NotificationDto> Items, int Page, int PageSize, int Total, int UnreadCount);

public sealed record UnreadCountDto(int UnreadCount);

public sealed record MarkReadRequest(Guid[]? Ids);

// ----- admin -----

public sealed record NotificationEventDto(string Code, NotificationCategory Category, bool IsCritical, string Recipients,
    IReadOnlyList<NotificationChannel> DefaultChannels, IReadOnlyList<NotificationChannel> AllowedChannels, IReadOnlyList<string> Placeholders, string? DeepLink);

public sealed record NotificationTemplateDto(Guid Id, string Code, NotificationChannel Channel, string? TitleAr, string? TitleEn, string BodyAr, string BodyEn,
    bool IsActive, DateTime UpdatedAt, string? UpdatedByName);

public sealed record CreateTemplateRequest(string? Code, NotificationChannel? Channel, string? TitleAr, string? TitleEn, string? BodyAr, string? BodyEn, bool? IsActive);

public sealed record UpdateTemplateRequest(string? TitleAr, string? TitleEn, string? BodyAr, string? BodyEn, bool? IsActive);

public sealed record PreviewTemplateRequest(Dictionary<string, string?>? Placeholders, string? Language);

public sealed record TemplatePreviewDto(string? Title, string Body, int Length, int SmsSegments);

public sealed record TestTemplateRequest(Guid? UserId);

public sealed record CampaignAudience(
    List<string>? Roles, List<Guid>? CityIds, List<string>? Languages, List<string>? Genders, List<string>? DriverTiers,
    int? LastActiveWithinDays, bool? HasCompletedTrip, List<Guid>? UserIds);

public sealed record CampaignUpsertRequest(string? Name, NotificationCategory? Category, List<NotificationChannel>? Channels, CampaignAudience? Audience,
    string? TitleAr, string? TitleEn, string? BodyAr, string? BodyEn, string? DeepLink);

public sealed record CampaignDto(Guid Id, string Name, NotificationCategory Category, IReadOnlyList<NotificationChannel> Channels, CampaignAudience Audience,
    string TitleAr, string TitleEn, string BodyAr, string BodyEn, string? DeepLink, CampaignStatus Status, DateTime? ScheduledAt, DateTime? StartedAt,
    DateTime? CompletedAt, int TargetCount, int InappCreated, int PushSent, int PushFailed, int PushSkipped, int SmsSent, int SmsFailed, int OpenedCount,
    DateTime CreatedAt, DateTime UpdatedAt)
{
    public string? CreatedByName { get; init; }
}

public sealed record ScheduleCampaignRequest(DateTime? ScheduledAt);

public sealed record AudiencePreviewRequest(CampaignAudience? Audience);

public sealed record AudienceSampleDto(Guid UserId, string? Name, string PhoneMasked);

public sealed record AudiencePreviewDto(int Count, IReadOnlyList<AudienceSampleDto> Sample);

public sealed record DeliveryDto(Guid Id, string EventCode, NotificationChannel Channel, DeliveryStatus Status, string? SkippedReason, Guid? UserId, Guid? CampaignId, string? UserName,
    string? PhoneMasked, string Provider, string? ProviderMessageId, string? ErrorCode, string? ErrorMessage, int Attempts, DateTime? SentAt, DateTime? OpenedAt,
    DateTime CreatedAt, JsonElement? Payload);

public sealed record DutyDto(bool OnDuty);
