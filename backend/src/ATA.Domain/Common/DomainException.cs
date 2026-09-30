namespace ATA.Domain.Common;

/// <summary>
/// A business-rule violation. <see cref="Code"/> is one of <see cref="ErrorCodes"/>; the API layer maps the code to an
/// HTTP status and a localized message.
/// </summary>
/// <param name="status">Optional HTTP status overriding the catalogue's (e.g. <c>400 invalid_credentials</c> for a wrong current password, which must not look like an expired session).</param>
public sealed class DomainException(string code, object? details = null, int? status = null) : Exception(code)
{
    public string Code { get; } = code;
    public object? Details { get; } = details;
    public int? Status { get; } = status;
}
