namespace ATA.Domain.Common;

/// <summary>
/// A business-rule violation. <see cref="Code"/> is one of <see cref="ErrorCodes"/>; the API layer maps the code to an
/// HTTP status and a localized message.
/// </summary>
public sealed class DomainException(string code, object? details = null) : Exception(code)
{
    public string Code { get; } = code;
    public object? Details { get; } = details;
}
