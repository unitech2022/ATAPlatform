using ATA.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ATA.Infrastructure.Persistence.Seed;

/// <summary>
/// Startup sync of the permission catalogue (doc 12 §F20.2): <c>permissions</c> mirrors <see cref="PermissionCatalog.Definitions"/> (added / renamed / removed),
/// missing system roles are created with their default permissions (existing roles keep what admins configured), and every active admin account without a role
/// whose legacy <c>permissions</c> JSON holds <c>*</c> receives <c>super_admin</c> (the legacy JSON is then cleared). Idempotent.
/// </summary>
public sealed class RbacSynchronizer(AtaDbContext db, ILogger<RbacSynchronizer> logger)
{
    public async Task SyncAsync(CancellationToken ct = default)
    {
        var existing = await db.Permissions.ToListAsync(ct);
        foreach (var definition in PermissionCatalog.Definitions)
        {
            var row = existing.FirstOrDefault(p => p.Code == definition.Code);
            if (row is null)
            {
                row = new Permission { Code = definition.Code, Module = definition.Module, NameAr = definition.NameAr, NameEn = definition.NameEn };
                db.Permissions.Add(row);
                existing.Add(row);
            }

            row.Module = definition.Module;
            row.NameAr = definition.NameAr;
            row.NameEn = definition.NameEn;
            row.Description = definition.Description;
            row.SortOrder = definition.SortOrder;
        }

        var obsolete = existing.Where(p => !PermissionCatalog.Codes.Contains(p.Code)).ToList();
        if (obsolete.Count > 0)
        {
            var ids = obsolete.Select(p => p.Id).ToList();
            db.RolePermissions.RemoveRange(await db.RolePermissions.Where(rp => ids.Contains(rp.PermissionId)).ToListAsync(ct));
            db.Permissions.RemoveRange(obsolete);
            logger.LogInformation("Removed {Count} permissions no longer in the catalogue: {Codes}", obsolete.Count, string.Join(", ", obsolete.Select(p => p.Code)));
        }

        await db.SaveChangesAsync(ct);

        var byCode = existing.Where(p => PermissionCatalog.Codes.Contains(p.Code)).ToDictionary(p => p.Code, p => p.Id);
        var roles = await db.Roles.ToListAsync(ct);
        foreach (var definition in PermissionCatalog.SystemRoles)
        {
            var role = roles.FirstOrDefault(r => r.Code == definition.Code);
            if (role is null)
            {
                role = new AdminRole { Code = definition.Code, NameAr = definition.NameAr, NameEn = definition.NameEn, Description = definition.Description, IsSystem = true };
                db.Roles.Add(role);
                roles.Add(role);
                foreach (var code in definition.Permissions.Where(byCode.ContainsKey))
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = byCode[code] });
                }

                logger.LogInformation("Seeded system role '{Role}'", definition.Code);
            }
            else
            {
                role.IsSystem = true;
            }
        }

        await db.SaveChangesAsync(ct);

        var superAdmin = roles.First(r => r.Code == PermissionCatalog.SuperAdminRole);
        var withRoles = db.AdminAccountRoles.Select(r => r.AdminAccountId);
        var legacy = await db.AdminAccounts.Where(a => !withRoles.Contains(a.Id) && a.Permissions.Contains("\"*\"")).ToListAsync(ct);
        foreach (var account in legacy)
        {
            db.AdminAccountRoles.Add(new AdminAccountRole { AdminAccountId = account.Id, RoleId = superAdmin.Id });
            account.Permissions = "[]";
            logger.LogInformation("Granted super_admin to legacy admin '{Username}'", account.Username);
        }

        await db.SaveChangesAsync(ct);
    }
}
