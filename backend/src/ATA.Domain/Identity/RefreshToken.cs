using ATA.Domain.Common;

namespace ATA.Domain.Identity;

/// <summary>Which client a refresh token belongs to (<c>refresh_tokens.session_kind</c>): a corporate session keeps its <c>corp</c> claim when refreshed.</summary>
public enum SessionKind { App, Admin, Corporate }

public class RefreshToken : Entity
{
    public SessionKind SessionKind { get; set; } = SessionKind.App;
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public string? DeviceId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedById { get; set; }
    public string? CreatedByIp { get; set; }
    /// <summary>F20: shown in the admin session list.</summary>
    public string? UserAgent { get; set; }
    /// <summary>F20: last login / refresh of the session (admin idle limit).</summary>
    public DateTime? LastUsedAt { get; set; }
    /// <summary>F20: hard end of an admin session (<c>Admin:SessionAbsoluteHours</c> after the login), carried over by every rotation.</summary>
    public DateTime? AbsoluteExpiresAt { get; set; }

    public bool IsActive(DateTime now) => RevokedAt is null && now < ExpiresAt;
}
