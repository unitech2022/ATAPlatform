namespace ATA.Domain.Common;

/// <summary>Base entity: UUID v7 primary key generated in the application plus creation timestamp.</summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
}

/// <summary>Entity that also tracks its last modification time.</summary>
public abstract class AuditableEntity : Entity
{
    public DateTime UpdatedAt { get; set; }
}
