using ATA.Domain.Common;

namespace ATA.Domain.Admin;

public class AuditLog : Entity
{
    public Guid? ActorUserId { get; set; }
    public string? ActorRole { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? IpAddress { get; set; }
}
