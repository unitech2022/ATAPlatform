using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public class UserDevice : Entity
{
    public Guid UserId { get; set; }
    public required string DeviceId { get; set; }
    public DevicePlatform Platform { get; set; }
    public string? DeviceName { get; set; }
    public string? PushToken { get; set; }
    public string? AppVersion { get; set; }
    public DateTime LastSeenAt { get; set; }
}
