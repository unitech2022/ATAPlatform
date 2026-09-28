using Microsoft.Extensions.Logging;

namespace ATA.Infrastructure.Sms;

/// <summary>Development sender: writes the SMS to the log instead of a gateway.</summary>
public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public string Provider => "logging";

    public Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SMS to {Phone}: {Message}", phoneNumber, message);
        return Task.FromResult(new SmsSendResult(true, $"log-{Guid.NewGuid():N}", null, null, false));
    }
}
