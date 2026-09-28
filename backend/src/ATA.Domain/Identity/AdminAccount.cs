using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public class AdminAccount : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? MfaSecret { get; set; }
    public bool MfaEnabled { get; set; }
    /// <summary>JSON array of permission strings, e.g. <c>["*"]</c>.</summary>
    public string Permissions { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public User? User { get; set; }
}
