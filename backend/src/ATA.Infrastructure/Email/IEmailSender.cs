namespace ATA.Infrastructure.Email;

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public sealed record EmailMessage(string To, string Subject, string TextBody, IReadOnlyList<EmailAttachment>? Attachments = null);

public sealed record EmailSendResult(bool Success, string? ProviderMessageId, string? ErrorCode)
{
    public static EmailSendResult Sent(string? messageId = null) => new(true, messageId, null);

    public static EmailSendResult Failed(string code) => new(false, null, code);
}

/// <summary>
/// E-mail is used for exactly one purpose: corporate invoices to <c>corporate_accounts.billing_email</c> (doc 12 decision 5 — an exception to the "no e-mail" identity rule).
/// Only the <c>logging</c> provider exists in v1 (a placeholder for an SMTP / API adapter).
/// </summary>
public interface IEmailSender
{
    /// <summary>Provider name (<c>logging</c> until a real adapter is added).</summary>
    string Provider { get; }

    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed class EmailOptions
{
    public const string Section = "Email";
    /// <summary><c>logging</c> (default): the message is only written to the log, nothing is sent.</summary>
    public string Provider { get; set; } = "logging";
    public string From { get; set; } = "billing@ata.sa";
}
