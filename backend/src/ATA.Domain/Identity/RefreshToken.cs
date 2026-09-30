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

    public bool IsActive(DateTime now) => RevokedAt is null && now < ExpiresAt;
}
