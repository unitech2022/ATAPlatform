using ATA.Domain.Common;

namespace ATA.Domain.Notifications;

/// <summary>Category of an event: selects the user preference (see <see cref="NotificationEvents.PreferenceOf"/>) and the Android channel.</summary>
public enum NotificationCategory { Trips, Offers, Safety, Wallet, Promotions, System }

public enum NotificationChannel { Inapp, Push, Sms }

public enum DeliveryStatus { Queued, Sent, Failed, Skipped }

public static class DeliverySkipReasons
{
    public const string PreferenceOff = "preference_off";
    public const string NoSubscription = "no_subscription";
    public const string TemplateInactive = "template_inactive";
    public const string NoPhone = "no_phone";
    public const string UserInactive = "user_inactive";
}

public enum CampaignStatus { Draft, Scheduled, Sending, Sent, Cancelled, Failed }

/// <summary>Editable text of an event for one channel (<c>notification_templates</c>, <c>UNIQUE(code, channel)</c>).</summary>
public class NotificationTemplate : AuditableEntity
{
    public required string Code { get; set; }
    public NotificationChannel Channel { get; set; }
    public string? TitleAr { get; set; }
    public string? TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? UpdatedBy { get; set; }
}

/// <summary>One push/SMS send attempt record (<c>notification_deliveries</c>) processed by the delivery worker.</summary>
public class NotificationDelivery : AuditableEntity
{
    public Guid? NotificationId { get; set; }
    public Guid? UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public required string EventCode { get; set; }
    public NotificationChannel Channel { get; set; }
    public Guid? CampaignId { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Queued;
    public string? SkippedReason { get; set; }
    public required string Provider { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public byte Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    /// <summary>The message as sent (JSON).</summary>
    public required string Payload { get; set; }

    public void Skip(string reason)
    {
        Status = DeliveryStatus.Skipped;
        SkippedReason = reason;
        NextAttemptAt = null;
    }
}

/// <summary>A broadcast to an audience (<c>notification_campaigns</c>).</summary>
public class NotificationCampaign : AuditableEntity
{
    public required string Name { get; set; }
    public NotificationCategory Category { get; set; } = NotificationCategory.Promotions;
    /// <summary>JSON array of channels, e.g. <c>["inapp","push"]</c>.</summary>
    public string Channels { get; set; } = "[\"inapp\",\"push\"]";
    /// <summary>JSON audience filter (roles, cityIds, languages, genders, driverTiers, lastActiveWithinDays, hasCompletedTrip, userIds).</summary>
    public string Audience { get; set; } = "{}";
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    public string? DeepLink { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TargetCount { get; set; }
    public int InappCreated { get; set; }
    public int PushSent { get; set; }
    public int PushFailed { get; set; }
    public int PushSkipped { get; set; }
    public int SmsSent { get; set; }
    public int SmsFailed { get; set; }
    public int OpenedCount { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsEditable => Status == CampaignStatus.Draft;

    public void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new DomainException(ErrorCodes.CampaignNotEditable, new { status = Status });
        }
    }
}

/// <summary>Marks a document-expiry notice as sent once per offset (<c>UNIQUE(driver_document_id, offset_days)</c>; 0 = expired).</summary>
public class DocumentExpiryNotice
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DriverDocumentId { get; set; }
    public int OffsetDays { get; set; }
    public DateTime SentAt { get; set; }
}
