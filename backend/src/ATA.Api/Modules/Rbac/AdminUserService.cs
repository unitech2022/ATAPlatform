using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Identity;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Rbac;

/// <summary>
/// <c>/admin/admin-users</c> (<c>admin.users.manage</c>, doc 12 §F20.5): create / link, roles, disable / enable, password and MFA resets, unlock and session
/// revocation. Rules (§F20.3): a role change, disabling, a password or MFA reset revokes every refresh token of the user; nobody can disable themselves
/// (<c>409 conflict { reason: "cannot_disable_self" }</c>); the last active <c>super_admin</c> cannot lose the role or be disabled
/// (<c>409 conflict { reason: "last_super_admin" }</c>); granting or removing <c>super_admin</c> needs <c>*</c>.
/// </summary>
public sealed partial class AdminUserService(
    AtaDbContext db, ICurrentUser currentUser, IPasswordHasher hasher, IClock clock, AdminMfaService mfa, AdminRbacQueries queries, AuditService audit)
{
    public async Task<PagedResult<AdminUserDto>> ListAsync(string? search, Guid? roleId, bool? isActive, Paging paging, Language lang, CancellationToken ct)
    {
        var query = db.AdminAccounts.AsNoTracking().Include(a => a.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(a => a.Username.Contains(term) || a.User!.PhoneNumber.Contains(phone) || (a.User.FullName != null && a.User.FullName.Contains(term)));
        }

        if (roleId is { } role)
        {
            var holders = db.AdminAccountRoles.Where(r => r.RoleId == role).Select(r => r.AdminAccountId);
            query = query.Where(a => holders.Contains(a.Id));
        }

        if (isActive is { } active)
        {
            query = query.Where(a => a.IsActive == active);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(a => a.Username).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var roles = await queries.RoleRefsAsync(rows.Select(r => r.Id).ToList(), lang, ct);
        return paging.Result(rows.Select(a => ToDto(a, roles.GetValueOrDefault(a.Id))).ToList(), total);
    }

    public async Task<AdminUserDetailDto> GetAsync(Guid id, Language lang, CancellationToken ct)
    {
        var account = await db.AdminAccounts.AsNoTracking().Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var roles = (await queries.RoleRefsAsync([id], lang, ct)).GetValueOrDefault(id) ?? [];
        var permissions = await AdminPermissions.OfAccountAsync(db, id, ct);
        var sessions = await queries.SessionsAsync(account.UserId, null, ct);
        return new AdminUserDetailDto(account.Id, account.UserId, account.Username, account.User!.FullName, account.User.PhoneNumber, roles, account.IsActive, account.MfaEnabled,
            account.LastLoginAt, account.OnDuty, account.LockedUntil, account.MustChangePassword, account.MfaEnrolledAt, account.PasswordChangedAt, account.FailedLoginCount,
            account.CreatedAt, permissions, sessions);
    }

    /// <summary>Creates (or links, by phone number) the user with the <c>admin</c> role and an admin account that must change its temporary password.</summary>
    public async Task<CreateAdminUserResponse> CreateAsync(CreateAdminUserRequest request, Language lang, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.Username), request.Username, 64)
            .Require(nameof(request.FullName), request.FullName, 120)
            .Require(nameof(request.PhoneNumber), request.PhoneNumber, 20)
            .Rule(nameof(request.RoleIds), request.RoleIds is { Count: > 0 }, "at least one role is required");
        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            v.Rule(nameof(request.Username), UsernamePattern().IsMatch(request.Username.Trim()), "3-64 characters: a-z, 0-9, dot, underscore or dash");
        }

        v.ThrowIfInvalid();
        var phone = PhoneNumber.Normalize(request.PhoneNumber);
        var temporaryPassword = request.TemporaryPassword;
        if (temporaryPassword is null)
        {
            temporaryPassword = PasswordPolicy.GenerateTemporary();
        }
        else
        {
            PasswordPolicy.Validate(temporaryPassword);
        }

        var username = request.Username!.Trim();
        if (await db.AdminAccounts.AnyAsync(a => a.Username == username, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { field = "username" });
        }

        var roleIds = await ResolveRolesAsync(request.RoleIds!, ct);
        await EnsureCanChangeSuperAdminAsync(new HashSet<Guid>(), roleIds, ct);

        var now = clock.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        if (user is null)
        {
            user = new User { PhoneNumber = phone, Language = Language.Ar };
            db.Users.Add(user);
        }
        else if (await db.AdminAccounts.AnyAsync(a => a.UserId == user.Id, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { field = "phoneNumber" });
        }

        user.FullName = request.FullName!.Trim();
        if (!user.HasRole(Role.Admin))
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Admin, GrantedAt = now });
        }

        var account = new AdminAccount
        {
            UserId = user.Id, Username = username, PasswordHash = hasher.Hash(temporaryPassword), MustChangePassword = true, IsActive = true, User = user,
        };
        db.AdminAccounts.Add(account);
        foreach (var roleId in roleIds)
        {
            db.AdminAccountRoles.Add(new AdminAccountRole { AdminAccountId = account.Id, RoleId = roleId });
        }

        audit.Log("admin_user.create", "admin_account", account.Id, null, new { username, phoneNumber = phone, roleIds });
        await db.SaveChangesAsync(ct);
        var roles = (await queries.RoleRefsAsync([account.Id], lang, ct)).GetValueOrDefault(account.Id);
        return new CreateAdminUserResponse(ToDto(account, roles), temporaryPassword);
    }

    public async Task<AdminUserDto> UpdateAsync(Guid id, UpdateAdminUserRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.FullName), request.FullName, 120)
            .Rule(nameof(request.RoleIds), request.RoleIds is { Count: > 0 }, "at least one role is required")
            .ThrowIfInvalid();

        var account = await LoadAsync(id, ct);
        var current = await db.AdminAccountRoles.Where(r => r.AdminAccountId == id).ToListAsync(ct);
        var currentIds = current.Select(r => r.RoleId).ToHashSet();
        var requested = await ResolveRolesAsync(request.RoleIds!, ct);
        await EnsureCanChangeSuperAdminAsync(currentIds, requested, ct);
        var superAdmin = await SuperAdminRoleIdAsync(ct);
        if (account.IsActive && currentIds.Contains(superAdmin) && !requested.Contains(superAdmin) && !await OtherActiveSuperAdminsAsync(id, superAdmin, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "last_super_admin" });
        }

        var before = new { fullName = account.User!.FullName, roleIds = currentIds.OrderBy(x => x).ToArray() };
        account.User.FullName = request.FullName!.Trim();
        var rolesChanged = !currentIds.SetEquals(requested);
        db.AdminAccountRoles.RemoveRange(current.Where(r => !requested.Contains(r.RoleId)));
        foreach (var roleId in requested.Where(r => !currentIds.Contains(r)))
        {
            db.AdminAccountRoles.Add(new AdminAccountRole { AdminAccountId = id, RoleId = roleId });
        }

        audit.Log("admin_user.update", "admin_account", id, before, new { fullName = account.User.FullName, roleIds = requested.OrderBy(x => x).ToArray() });
        await db.SaveChangesAsync(ct);
        if (rolesChanged)
        {
            await queries.RevokeAllAsync(account.UserId, null, ct);
        }

        return await DtoAsync(account, lang, ct);
    }

    public async Task<AdminUserDto> DisableAsync(Guid id, Language lang, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        if (account.UserId == currentUser.UserId)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "cannot_disable_self" });
        }

        if (account.IsActive)
        {
            var superAdmin = await SuperAdminRoleIdAsync(ct);
            if (await db.AdminAccountRoles.AnyAsync(r => r.AdminAccountId == id && r.RoleId == superAdmin, ct) && !await OtherActiveSuperAdminsAsync(id, superAdmin, ct))
            {
                throw new DomainException(ErrorCodes.Conflict, new { reason = "last_super_admin" });
            }

            account.IsActive = false;
            account.OnDuty = false;
            audit.Log("admin_user.disable", "admin_account", id, new { isActive = true }, new { isActive = false });
            await db.SaveChangesAsync(ct);
        }

        await queries.RevokeAllAsync(account.UserId, null, ct);
        return await DtoAsync(account, lang, ct);
    }

    public async Task<AdminUserDto> EnableAsync(Guid id, Language lang, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        if (!account.IsActive)
        {
            account.IsActive = true;
            audit.Log("admin_user.enable", "admin_account", id, new { isActive = false }, new { isActive = true });
            await db.SaveChangesAsync(ct);
        }

        return await DtoAsync(account, lang, ct);
    }

    /// <summary>A new temporary password (shown once) with <c>must_change_password</c>; the lockout is cleared and all sessions are revoked.</summary>
    public async Task<TemporaryPasswordResponse> ResetPasswordAsync(Guid id, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        var temporary = PasswordPolicy.GenerateTemporary();
        account.PasswordHash = hasher.Hash(temporary);
        account.MustChangePassword = true;
        account.FailedLoginCount = 0;
        account.LockedUntil = null;
        audit.Log("admin_user.reset_password", "admin_account", id);
        await db.SaveChangesAsync(ct);
        await queries.RevokeAllAsync(account.UserId, null, ct);
        return new TemporaryPasswordResponse(temporary);
    }

    public async Task<AdminUserDto> ResetMfaAsync(Guid id, Language lang, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        var before = new { account.MfaEnabled, account.MfaEnrolledAt };
        await mfa.ResetAsync(account, ct);
        audit.Log("admin_user.reset_mfa", "admin_account", id, before, new { account.MfaEnabled });
        await db.SaveChangesAsync(ct);
        await queries.RevokeAllAsync(account.UserId, null, ct);
        return await DtoAsync(account, lang, ct);
    }

    public async Task<AdminUserDto> UnlockAsync(Guid id, Language lang, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        var before = new { account.LockedUntil, account.FailedLoginCount, account.MfaFailedCount };
        account.LockedUntil = null;
        account.FailedLoginCount = 0;
        account.MfaFailedCount = 0;
        audit.Log("admin_user.unlock", "admin_account", id, before, new { account.LockedUntil });
        await db.SaveChangesAsync(ct);
        return await DtoAsync(account, lang, ct);
    }

    public async Task RevokeSessionsAsync(Guid id, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        var revoked = await queries.RevokeAllAsync(account.UserId, null, ct);
        audit.Log("admin_user.revoke_sessions", "admin_account", id, null, new { revoked });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AdminSessionDto>> SessionsAsync(Guid id, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        return await queries.SessionsAsync(account.UserId, currentUser.UserId == account.UserId ? currentUser.SessionId : null, ct);
    }

    public async Task RevokeSessionAsync(Guid id, Guid sessionId, CancellationToken ct)
    {
        var account = await LoadAsync(id, ct);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == account.UserId && t.SessionKind == SessionKind.Admin, ct)
            ?? throw new DomainException(ErrorCodes.NotFound);
        token.RevokedAt ??= clock.UtcNow;
        audit.Log("admin_user.revoke_sessions", "admin_account", id, null, new { sessionId });
        await db.SaveChangesAsync(ct);
    }

    private async Task<AdminAccount> LoadAsync(Guid id, CancellationToken ct) =>
        await db.AdminAccounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw new DomainException(ErrorCodes.NotFound);

    private async Task<HashSet<Guid>> ResolveRolesAsync(IReadOnlyList<Guid> roleIds, CancellationToken ct)
    {
        var requested = roleIds.Distinct().ToList();
        var existing = await db.Roles.AsNoTracking().Where(r => requested.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        var unknown = requested.Except(existing).ToList();
        if (unknown.Count > 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["roleIds"] = "unknown role: " + string.Join(',', unknown) });
        }

        return existing.ToHashSet();
    }

    private Task<Guid> SuperAdminRoleIdAsync(CancellationToken ct) =>
        db.Roles.AsNoTracking().Where(r => r.Code == PermissionCatalog.SuperAdminRole).Select(r => r.Id).FirstAsync(ct);

    private Task<bool> OtherActiveSuperAdminsAsync(Guid exceptAccountId, Guid superAdminRoleId, CancellationToken ct) =>
        (from ar in db.AdminAccountRoles
         join a in db.AdminAccounts on ar.AdminAccountId equals a.Id
         where ar.RoleId == superAdminRoleId && a.IsActive && a.Id != exceptAccountId
         select a.Id).AnyAsync(ct);

    /// <summary>Only a caller holding <c>*</c> may grant or remove <c>super_admin</c> (<c>403 forbidden { permission: "*" }</c>).</summary>
    private async Task EnsureCanChangeSuperAdminAsync(IReadOnlySet<Guid> before, IReadOnlySet<Guid> after, CancellationToken ct)
    {
        var superAdmin = await SuperAdminRoleIdAsync(ct);
        if (before.Contains(superAdmin) != after.Contains(superAdmin) && !currentUser.HasPermission(PermissionCatalog.All))
        {
            throw new DomainException(ErrorCodes.Forbidden, new { permission = PermissionCatalog.All });
        }
    }

    private async Task<AdminUserDto> DtoAsync(AdminAccount account, Language lang, CancellationToken ct) =>
        ToDto(account, (await queries.RoleRefsAsync([account.Id], lang, ct)).GetValueOrDefault(account.Id));

    private static AdminUserDto ToDto(AdminAccount a, IReadOnlyList<RoleRefDto>? roles) =>
        new(a.Id, a.UserId, a.Username, a.User?.FullName, a.User?.PhoneNumber ?? string.Empty, roles ?? [], a.IsActive, a.MfaEnabled, a.LastLoginAt, a.OnDuty, a.LockedUntil);

    [GeneratedRegex("^[a-z0-9._-]{3,64}$", RegexOptions.IgnoreCase)]
    private static partial Regex UsernamePattern();
}
