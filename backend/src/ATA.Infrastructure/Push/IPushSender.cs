namespace ATA.Infrastructure.Push;

/// <summary>Sends a push notification to users addressed by their OneSignal External ID (= <c>users.id</c>).</summary>
public interface IPushSender
{
    /// <summary>Provider name stored in <c>notification_deliveries.provider</c> (<c>onesignal</c> / <c>logging</c>).</summary>
    string Provider { get; }

    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken ct);
}

/// <param name="ExternalUserIds">At most <c>Notifications:PushBatchSize</c> (2000) users.</param>
/// <param name="Headings">Title per language (<c>ar</c>, <c>en</c>); OneSignal requires <c>en</c>.</param>
/// <param name="Category">trips|offers|safety|wallet|promotions|system → <c>android_channel_id</c>.</param>
/// <param name="Priority"><c>high</c> or <c>normal</c>.</param>
public sealed record PushMessage(
    IReadOnlyList<Guid> ExternalUserIds,
    IReadOnlyDictionary<string, string> Headings,
    IReadOnlyDictionary<string, string> Contents,
    IReadOnlyDictionary<string, object?> Data,
    string Category,
    string Priority,
    int? TtlSeconds,
    string? CollapseId,
    IReadOnlyList<PushButton>? Buttons,
    string IdempotencyKey);

public sealed record PushButton(string Id, string TextAr, string TextEn);

public sealed record PushSendResult(
    bool Success,
    string? ProviderMessageId,
    int Recipients,
    IReadOnlyList<Guid> InvalidExternalUserIds,
    string? ErrorCode,
    string? ErrorMessage,
    bool IsTransient)
{
    public static PushSendResult Transient(string code, string? message) => new(false, null, 0, [], code, message, true);

    public static PushSendResult Permanent(string code, string? message) => new(false, null, 0, [], code, message, false);
}

public sealed class OneSignalOptions
{
    public const string Section = "OneSignal";
    /// <summary><c>false</c> in development/tests: <see cref="LoggingPushSender"/> is used. Also falls back to logging when keys are missing.</summary>
    public bool Enabled { get; set; }
    public string? AppId { get; set; }
    public string? RestApiKey { get; set; }
    public string ApiBaseUrl { get; set; } = "https://api.onesignal.com";
    public int TimeoutSeconds { get; set; } = 10;
    /// <summary>Android notification channel ids created in the OneSignal dashboard, per category (trips, offers, safety, wallet, promotions, system).</summary>
    public Dictionary<string, string> AndroidChannels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(RestApiKey);
}
