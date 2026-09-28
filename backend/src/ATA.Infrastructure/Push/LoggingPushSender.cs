using Microsoft.Extensions.Logging;

namespace ATA.Infrastructure.Push;

/// <summary>Development/test sender (<c>OneSignal:Enabled=false</c> or no keys): logs the message and reports success.</summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> logger) : IPushSender
{
    public string Provider => "logging";

    public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken ct)
    {
        logger.LogInformation("Push to {Count} user(s) [{Category}] {Heading}: {Content}",
            message.ExternalUserIds.Count, message.Category, message.Headings.GetValueOrDefault("en"), message.Contents.GetValueOrDefault("en"));
        return Task.FromResult(new PushSendResult(true, $"log-{Guid.NewGuid():N}", message.ExternalUserIds.Count, [], null, null, false));
    }
}
