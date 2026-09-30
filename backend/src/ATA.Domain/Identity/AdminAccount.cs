using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public class AdminAccount : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? MfaSecret { get; set; }
    public bool MfaEnabled { get; set; }
    /// <summary>Deprecated by F20 (kept for compatibility only): permissions now derive from <c>admin_account_roles</c>.</summary>
    public string Permissions { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    /// <summary>On-duty operators receive <c>safety.alert</c> pushes.</summary>
    public bool OnDuty { get; set; }

    // ----- F20 (doc 12 §F20.1 / §F20.4) -----
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    /// <summary>Set for a temporary password: only <c>POST /admin/me/password</c> is allowed until it changes (<c>403 password_change_required</c>).</summary>
    public bool MustChangePassword { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime? MfaEnrolledAt { get; set; }
    /// <summary>Last accepted TOTP time step: a code of this step or an earlier one is rejected (replay guard).</summary>
    public long? MfaLastStep { get; set; }
    public int MfaFailedCount { get; set; }

    public bool IsLockedAt(DateTime now) => LockedUntil is { } until && until > now;

    public User? User { get; set; }
}
