using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Rbac;

/// <summary>
/// <c>/admin/roles</c> and <c>/admin/permissions</c> (<c>admin.roles.manage</c>, doc 12 §F20.5). System roles cannot be deleted and keep their code
/// (<c>409 conflict { reason: "system_role" }</c>); <c>super_admin</c> always grants <c>*</c> (its permission list cannot be edited); a role still assigned
/// cannot be deleted (<c>409 conflict { reason: "role_in_use", userCount }</c>). Permission codes must come from the catalogue.
/// </summary>
public sealed partial class RoleService(AtaDbContext db, AuditService audit)
{
    public async Task<IReadOnlyList<PermissionDto>> PermissionsAsync(Language lang, CancellationToken ct)
    {
        var rows = await db.Permissions.AsNoTracking().OrderBy(p => p.SortOrder).ThenBy(p => p.Code).ToListAsync(ct);
        return rows.Select(p => new PermissionDto(p.Code, p.Module, lang.Pick(p.NameAr, p.NameEn), p.NameAr, p.NameEn, p.Description)).ToList();
    }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(Language lang, CancellationToken ct)
    {
        var roles = await db.Roles.AsNoTracking().OrderByDescending(r => r.IsSystem).ThenBy(r => r.Code).ToListAsync(ct);
        return await DtosAsync(roles, lang, ct);
    }

    public async Task<RoleDto> GetAsync(Guid id, Language lang, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return (await DtosAsync([role], lang, ct))[0];
    }

    public async Task<RoleDto> CreateAsync(RoleUpsertRequest request, Language lang, CancellationToken ct)
    {
        var codes = Validate(request, requireCode: true);
        var code = request.Code!.Trim();
        if (await db.Roles.AnyAsync(r => r.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { field = "code" });
        }

        var role = new AdminRole { Code = code, NameAr = request.NameAr!.Trim(), NameEn = request.NameEn!.Trim(), Description = Clean(request.Description) };
        db.Roles.Add(role);
        await SetPermissionsAsync(role, codes, ct);
        audit.Log("role.create", "role", role.Id, null, new { role.Code, role.NameAr, role.NameEn, permissionCodes = codes });
        await db.SaveChangesAsync(ct);
        return await GetAsync(role.Id, lang, ct);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, RoleUpsertRequest request, Language lang, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var codes = Validate(request, requireCode: false);
        var code = string.IsNullOrWhiteSpace(request.Code) ? role.Code : request.Code.Trim();
        if (code != role.Code)
        {
            if (role.IsSystem)
            {
                throw new DomainException(ErrorCodes.Conflict, new { reason = "system_role" });
            }

            if (await db.Roles.AnyAsync(r => r.Code == code && r.Id != id, ct))
            {
                throw new DomainException(ErrorCodes.Conflict, new { field = "code" });
            }
        }

        var before = await SnapshotAsync(role, ct);
        role.Code = code;
        role.NameAr = request.NameAr!.Trim();
        role.NameEn = request.NameEn!.Trim();
        role.Description = Clean(request.Description);
        if (role.IsSuperAdmin)
        {
            if (codes.Count > 0 && !(codes.Count == 1 && codes[0] == PermissionCatalog.All))
            {
                throw new DomainException(ErrorCodes.Conflict, new { reason = "system_role" });
            }
        }
        else
        {
            await SetPermissionsAsync(role, codes, ct);
        }

        audit.Log("role.update", "role", role.Id, before, new { role.Code, role.NameAr, role.NameEn, role.Description, permissionCodes = role.IsSuperAdmin ? [PermissionCatalog.All] : codes });
        await db.SaveChangesAsync(ct);
        return await GetAsync(role.Id, lang, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (role.IsSystem)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "system_role" });
        }

        var userCount = await db.AdminAccountRoles.CountAsync(r => r.RoleId == id, ct);
        if (userCount > 0)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "role_in_use", userCount });
        }

        var before = await SnapshotAsync(role, ct);
        db.RolePermissions.RemoveRange(await db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync(ct));
        db.Roles.Remove(role);
        audit.Log("role.delete", "role", id, before);
        await db.SaveChangesAsync(ct);
    }

    private static List<string> Validate(RoleUpsertRequest request, bool requireCode)
    {
        var v = new Validator()
            .Require(nameof(request.NameAr), request.NameAr, 100)
            .Require(nameof(request.NameEn), request.NameEn, 100)
            .Rule(nameof(request.Description), request.Description is null || request.Description.Length <= 255, "max_length:255");
        if (requireCode)
        {
            v.Require(nameof(request.Code), request.Code, 50);
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            v.Rule(nameof(request.Code), CodePattern().IsMatch(request.Code.Trim()), "2-50 characters: a-z, 0-9 or underscore, starting with a letter");
        }

        var codes = (request.PermissionCodes ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToList();
        var unknown = codes.Where(c => c != PermissionCatalog.All && !PermissionCatalog.Codes.Contains(c)).ToList();
        if (unknown.Count > 0)
        {
            v.Fail(nameof(request.PermissionCodes), "unknown permission: " + string.Join(',', unknown));
        }

        v.ThrowIfInvalid();
        return codes;
    }

    /// <summary>Replaces the role's permissions; <c>*</c> is reserved to <c>super_admin</c>.</summary>
    private async Task SetPermissionsAsync(AdminRole role, IReadOnlyList<string> codes, CancellationToken ct)
    {
        if (codes.Contains(PermissionCatalog.All))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["permissionCodes"] = "* is reserved to super_admin" });
        }

        var ids = await db.Permissions.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToListAsync(ct);
        var existing = await db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing.Where(rp => !ids.Contains(rp.PermissionId)));
        foreach (var permissionId in ids.Where(pid => existing.All(rp => rp.PermissionId != pid)))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permissionId });
        }
    }

    private async Task<object> SnapshotAsync(AdminRole role, CancellationToken ct) => new
    {
        role.Code, role.NameAr, role.NameEn, role.Description,
        permissionCodes = role.IsSuperAdmin ? [PermissionCatalog.All] : await CodesOfAsync(role.Id, ct),
    };

    private async Task<string[]> CodesOfAsync(Guid roleId, CancellationToken ct) =>
        (await (from rp in db.RolePermissions.AsNoTracking() join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where rp.RoleId == roleId orderby p.SortOrder select p.Code).ToListAsync(ct)).ToArray();

    private async Task<List<RoleDto>> DtosAsync(IReadOnlyList<AdminRole> roles, Language lang, CancellationToken ct)
    {
        var ids = roles.Select(r => r.Id).ToList();
        var permissions = await (from rp in db.RolePermissions.AsNoTracking()
                                 join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                                 where ids.Contains(rp.RoleId)
                                 select new { rp.RoleId, p.Code, p.SortOrder }).ToListAsync(ct);
        var counts = await db.AdminAccountRoles.AsNoTracking().Where(r => ids.Contains(r.RoleId)).GroupBy(r => r.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);
        return roles.Select(r => new RoleDto(
            r.Id, r.Code, lang.Pick(r.NameAr, r.NameEn), r.NameAr, r.NameEn, r.Description, r.IsSystem,
            r.IsSuperAdmin ? [PermissionCatalog.All] : permissions.Where(p => p.RoleId == r.Id).OrderBy(p => p.SortOrder).Select(p => p.Code).ToList(),
            counts.GetValueOrDefault(r.Id), r.CreatedAt, r.UpdatedAt)).ToList();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("^[a-z][a-z0-9_]{1,49}$")]
    private static partial Regex CodePattern();
}
