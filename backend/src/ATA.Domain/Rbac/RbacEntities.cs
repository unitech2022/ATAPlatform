using ATA.Domain.Common;

namespace ATA.Domain.Rbac;

/// <summary>Row of <c>roles</c> (doc 12 §F20.1). A system role (<see cref="IsSystem"/>) cannot be deleted and its code cannot change.</summary>
public class AdminRole : AuditableEntity
{
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    /// <summary><c>super_admin</c> grants <c>*</c>; it has no <c>role_permissions</c> rows and its permission set cannot be edited.</summary>
    public bool IsSuperAdmin => Code == PermissionCatalog.SuperAdminRole;
}

/// <summary>Row of <c>permissions</c>: a read-only copy of <see cref="PermissionCatalog"/> synced at startup.</summary>
public class Permission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Code { get; set; }
    public required string Module { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class RolePermission
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public class AdminAccountRole : Entity
{
    public Guid AdminAccountId { get; set; }
    public Guid RoleId { get; set; }
}

/// <summary>A single-use MFA recovery code (<c>xxxx-xxxx</c>), stored hashed.</summary>
public class AdminRecoveryCode : Entity
{
    public Guid AdminAccountId { get; set; }
    public required string CodeHash { get; set; }
    public DateTime? UsedAt { get; set; }
}
