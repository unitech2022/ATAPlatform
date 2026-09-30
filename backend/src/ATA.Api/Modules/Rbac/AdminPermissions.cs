using ATA.Domain.Rbac;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Rbac;

/// <summary>Resolves admin permissions from roles (doc 12 §F20.3): the union of the account's role permissions, or <c>["*"]</c> for a <c>super_admin</c>.</summary>
public static class AdminPermissions
{
    public static async Task<IReadOnlyList<string>> OfAccountAsync(AtaDbContext db, Guid adminAccountId, CancellationToken ct)
    {
        var roleIds = await db.AdminAccountRoles.AsNoTracking().Where(r => r.AdminAccountId == adminAccountId).Select(r => r.RoleId).ToListAsync(ct);
        return await OfRolesAsync(db, roleIds, ct);
    }

    public static async Task<IReadOnlyList<string>> OfRolesAsync(AtaDbContext db, IReadOnlyCollection<Guid> roleIds, CancellationToken ct)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        if (await db.Roles.AsNoTracking().AnyAsync(r => roleIds.Contains(r.Id) && r.Code == PermissionCatalog.SuperAdminRole, ct))
        {
            return [PermissionCatalog.All];
        }

        var codes = await (from rp in db.RolePermissions.AsNoTracking()
                           join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                           where roleIds.Contains(rp.RoleId)
                           select new { p.Code, p.SortOrder }).ToListAsync(ct);
        return codes.OrderBy(c => c.SortOrder).Select(c => c.Code).Distinct().ToArray();
    }

    /// <summary>Admin account ids whose roles grant <paramref name="permission"/> (directly or through <c>super_admin</c>).</summary>
    public static IQueryable<Guid> AccountsGranting(this AtaDbContext db, string permission) =>
        from ar in db.AdminAccountRoles
        join r in db.Roles on ar.RoleId equals r.Id
        where r.Code == PermissionCatalog.SuperAdminRole
              || (from rp in db.RolePermissions join p in db.Permissions on rp.PermissionId equals p.Id where rp.RoleId == r.Id && p.Code == permission select rp.Id).Any()
        select ar.AdminAccountId;
}
