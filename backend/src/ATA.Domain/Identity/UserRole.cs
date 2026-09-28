using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public class UserRole
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public DateTime GrantedAt { get; set; }
}
