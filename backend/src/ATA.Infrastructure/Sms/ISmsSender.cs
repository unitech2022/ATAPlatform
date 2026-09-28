namespace ATA.Infrastructure.Sms;

public interface ISmsSender
{
    /// <summary>Provider name stored in <c>notification_deliveries.provider</c> (<c>logging</c> / <c>unifonic</c> / <c>taqnyat</c>).</summary>
    string Provider { get; }

    Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}

public sealed record SmsSendResult(bool Success, string? ProviderMessageId, string? ErrorCode, string? ErrorMessage, bool IsTransient)
{
    public static SmsSendResult Transient(string code, string? message) => new(false, null, code, message, true);

    public static SmsSendResult Permanent(string code, string? message) => new(false, null, code, message, false);
}

public sealed class SmsOptions
{
    public const string Section = "Sms";
    /// <summary><c>logging</c> | <c>unifonic</c> | <c>taqnyat</c>.</summary>
    public string Provider { get; set; } = "logging";
    /// <summary>Forces <see cref="LoggingSmsSender"/> whatever the provider (no HTTP call is made).</summary>
    public bool Sandbox { get; set; } = true;
    public string SenderName { get; set; } = "ATA";
    public int TimeoutSeconds { get; set; } = 10;
    public UnifonicOptions Unifonic { get; set; } = new();
    public TaqnyatOptions Taqnyat { get; set; } = new();

    public sealed class UnifonicOptions
    {
        public string BaseUrl { get; set; } = "https://el.cloud.unifonic.com";
        public string? AppSid { get; set; }
    }

    public sealed class TaqnyatOptions
    {
        public string BaseUrl { get; set; } = "https://api.taqnyat.sa";
        public string? BearerToken { get; set; }
    }
}
