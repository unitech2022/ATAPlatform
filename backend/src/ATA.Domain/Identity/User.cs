using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public class User : AuditableEntity
{
    public required string PhoneNumber { get; set; }
    public DateTime? PhoneVerifiedAt { get; set; }
    public string? FullName { get; set; }
    public Language Language { get; set; } = Language.Ar;
    public Gender Gender { get; set; } = Gender.Unknown;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? TermsAcceptedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> Roles { get; set; } = [];

    public bool HasRole(Role role) => Roles.Any(r => r.Role == role);

    public void EnsureActive()
    {
        if (Status != UserStatus.Active)
        {
            throw new DomainException(ErrorCodes.AccountSuspended, new { status = Status.ToString().ToLowerInvariant() });
        }
    }
}
