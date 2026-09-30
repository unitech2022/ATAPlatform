using Microsoft.Extensions.Logging;

namespace ATA.Infrastructure.Email;

/// <summary>Default sender: logs the message (recipient masked, no body) and reports success; nothing leaves the process.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public string Provider => "logging";

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("E-mail (logging provider) to {Recipient}: {Subject} [{Attachments} attachment(s)]", Mask(message.To), message.Subject, message.Attachments?.Count ?? 0);
        return Task.FromResult(EmailSendResult.Sent($"log-{Guid.NewGuid():N}"));
    }

    private static string Mask(string address)
    {
        var at = address.IndexOf('@');
        return at <= 1 ? "***" : address[0] + "***" + address[at..];
    }
}
