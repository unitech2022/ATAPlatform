using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Rbac;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F20 roles and admin users (doc 12 §F20.3 / §F20.5): system-role protections, the last super admin, session revocation, temporary passwords.</summary>
public sealed class RbacAdminUserTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Api = "/api/v1";

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()!;

    private async Task<string> RoleIdAsync(HttpClient admin, string code) =>
        Str((await (await admin.GetAsync($"{Api}/admin/roles")).ReadJsonAsync()).EnumerateArray().Single(r => Str(r, "code") == code), "id");

    private async Task<(JsonElement Auth, HttpResponseMessage Response)> LoginAsync(string username, string password)
    {
        using var anonymous = fixture.CreateClient();
        var response = await anonymous.PostAsJsonAsync($"{Api}/auth/admin/login", new { username, password });
        await response.Content.LoadIntoBufferAsync();
        return (response.IsSuccessStatusCode ? await response.ReadJsonAsync() : default, response);
    }

    private async Task<JsonElement> CreateUserAsync(HttpClient admin, string username, string phone, params string[] roleIds)
    {
        var created = await admin.PostAsJsonAsync($"{Api}/admin/admin-users", new { username, fullName = $"User {username}", phoneNumber = phone, roleIds });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        return await created.ReadJsonAsync();
    }

    /// <summary>Signs in with the temporary password and sets a permanent one; returns the client of that session.</summary>
    private async Task<(HttpClient Client, JsonElement Auth)> ActivateAsync(string username, string temporary, string password = "Permanent#Pass1")
    {
        var (auth, _) = await LoginAsync(username, temporary);
        var client = fixture.CreateClient(Str(auth, "accessToken"));
        (await client.PostAsJsonAsync($"{Api}/admin/me/password", new { currentPassword = temporary, newPassword = password })).EnsureSuccessStatusCode();
        return (client, auth);
    }

    [Fact]
    public async Task Roles_crud_with_permission_matrix_and_system_role_protections()
    {
        using var admin = await fixture.LoginAdminAsync();
        var created = await admin.PostAsJsonAsync($"{Api}/admin/roles", new
        {
            code = "night_shift", nameAr = "المناوبة الليلية", nameEn = "Night shift", description = "Live map and trips",
            permissionCodes = new[] { PermissionCatalog.LiveView, PermissionCatalog.TripsView },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var role = await created.ReadJsonAsync();
        var id = Str(role, "id");
        Assert.False(role.GetProperty("isSystem").GetBoolean());
        Assert.Equal([PermissionCatalog.TripsView, PermissionCatalog.LiveView], role.GetProperty("permissionCodes").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal("المناوبة الليلية", Str(role, "name"));

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Api}/admin/roles", new { code = "night_shift", nameAr = "x", nameEn = "x", permissionCodes = Array.Empty<string>() })).StatusCode);
        var unknown = await admin.PostAsJsonAsync($"{Api}/admin/roles", new { code = "bad_role", nameAr = "x", nameEn = "x", permissionCodes = new[] { "trips.fly" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        var star = await admin.PostAsJsonAsync($"{Api}/admin/roles", new { code = "star_role", nameAr = "x", nameEn = "x", permissionCodes = new[] { "*" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, star.StatusCode);

        var updated = await (await admin.PutAsJsonAsync($"{Api}/admin/roles/{id}", new
        {
            code = "night_ops", nameAr = "المناوبة", nameEn = "Night ops", description = (string?)null, permissionCodes = new[] { PermissionCatalog.LiveView, PermissionCatalog.SafetyManage },
        })).ReadJsonAsync();
        Assert.Equal("night_ops", Str(updated, "code"));
        Assert.Equal([PermissionCatalog.LiveView, PermissionCatalog.SafetyManage], updated.GetProperty("permissionCodes").EnumerateArray().Select(p => p.GetString()));

        // System roles: no code change, no delete; super_admin keeps "*".
        var agent = await RoleIdAsync(admin, "support_agent");
        var rename = await admin.PutAsJsonAsync($"{Api}/admin/roles/{agent}", new { code = "agents", nameAr = "الدعم", nameEn = "Support", permissionCodes = new[] { PermissionCatalog.SupportView } });
        Assert.Equal(HttpStatusCode.Conflict, rename.StatusCode);
        Assert.Equal("system_role", (await rename.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var reshaped = await admin.PutAsJsonAsync($"{Api}/admin/roles/{agent}", new { code = "support_agent", nameAr = "الدعم", nameEn = "Support", permissionCodes = new[] { PermissionCatalog.SupportView } });
        Assert.Equal(HttpStatusCode.OK, reshaped.StatusCode);
        Assert.Equal([PermissionCatalog.SupportView], (await reshaped.ReadJsonAsync()).GetProperty("permissionCodes").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"{Api}/admin/roles/{agent}")).StatusCode);
        var superAdmin = await RoleIdAsync(admin, PermissionCatalog.SuperAdminRole);
        var narrowed = await admin.PutAsJsonAsync($"{Api}/admin/roles/{superAdmin}", new { nameAr = "مدير", nameEn = "Root", permissionCodes = new[] { PermissionCatalog.TripsView } });
        Assert.Equal(HttpStatusCode.Conflict, narrowed.StatusCode);
        var superDto = await (await admin.GetAsync($"{Api}/admin/roles/{superAdmin}")).ReadJsonAsync();
        Assert.Equal(["*"], superDto.GetProperty("permissionCodes").EnumerateArray().Select(p => p.GetString()));
        Assert.True(superDto.GetProperty("userCount").GetInt32() >= 1);

        // A role in use cannot be deleted; an unused one can.
        await CreateUserAsync(admin, "night.user", fixture.NextPhone(), id);
        var inUse = await admin.DeleteAsync($"{Api}/admin/roles/{id}");
        Assert.Equal(HttpStatusCode.Conflict, inUse.StatusCode);
        Assert.Equal("role_in_use", (await inUse.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var spare = Str(await (await admin.PostAsJsonAsync($"{Api}/admin/roles", new { code = "spare", nameAr = "x", nameEn = "x", permissionCodes = Array.Empty<string>() })).ReadJsonAsync(), "id");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Api}/admin/roles/{spare}")).StatusCode);

        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "role").Select(a => a.Action).ToListAsync());
        Assert.Contains("role.create", actions);
        Assert.Contains("role.update", actions);
        Assert.Contains("role.delete", actions);
    }

    [Fact]
    public async Task Created_admin_must_change_the_temporary_password_before_anything_else()
    {
        using var admin = await fixture.LoginAdminAsync();
        var analyst = await RoleIdAsync(admin, "analyst");
        var phone = fixture.NextPhone();
        var created = await CreateUserAsync(admin, "sara.analyst", phone, analyst);
        var temporary = Str(created, "temporaryPassword");
        Assert.True(temporary.Length >= 12);
        var user = created.GetProperty("adminUser");
        Assert.Equal("sara.analyst", Str(user, "username"));
        Assert.Equal("analyst", Str(user.GetProperty("roles")[0], "code"));
        Assert.True(user.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Api}/admin/admin-users", new { username = "sara.analyst", fullName = "x", phoneNumber = fixture.NextPhone(), roleIds = new[] { analyst } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Api}/admin/admin-users", new { username = "other.user", fullName = "x", phoneNumber = phone, roleIds = new[] { analyst } })).StatusCode);
        var weak = await admin.PostAsJsonAsync($"{Api}/admin/admin-users", new { username = "weak.user", fullName = "x", phoneNumber = fixture.NextPhone(), roleIds = new[] { analyst }, temporaryPassword = "short" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, weak.StatusCode);
        Assert.Equal("password_policy_violation", await weak.ErrorCodeAsync());

        var (auth, _) = await LoginAsync("sara.analyst", temporary);
        Assert.True(auth.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal([PermissionCatalog.DashboardView, PermissionCatalog.TripsView, PermissionCatalog.PricingView, PermissionCatalog.ReportsView, PermissionCatalog.ReportsExport],
            auth.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        using var client = fixture.CreateClient(Str(auth, "accessToken"));
        var gated = await client.GetAsync($"{Api}/admin/me");
        Assert.Equal(HttpStatusCode.Forbidden, gated.StatusCode);
        Assert.Equal("password_change_required", await gated.ErrorCodeAsync());
        Assert.Equal("password_change_required", await (await client.GetAsync($"{Api}/admin/trips")).ErrorCodeAsync());

        var wrongCurrent = await client.PostAsJsonAsync($"{Api}/admin/me/password", new { currentPassword = "Wrong#Password1", newPassword = "Permanent#Pass1" });
        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
        Assert.Equal("invalid_credentials", await wrongCurrent.ErrorCodeAsync());
        var policy = await client.PostAsJsonAsync($"{Api}/admin/me/password", new { currentPassword = temporary, newPassword = "alllowercase" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, policy.StatusCode);
        var rules = (await policy.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("rules").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Equal(["uppercase", "digit", "symbol"], rules);
        var reused = await client.PostAsJsonAsync($"{Api}/admin/me/password", new { currentPassword = temporary, newPassword = temporary });
        Assert.Equal("password_policy_violation", await reused.ErrorCodeAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync($"{Api}/admin/me/password", new { currentPassword = temporary, newPassword = "Permanent#Pass1" })).StatusCode);
        // The same session keeps working.
        var me = await (await client.GetAsync($"{Api}/admin/me")).ReadJsonAsync();
        Assert.False(me.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Api}/admin/trips")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"{Api}/admin/drivers")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("sara.analyst", temporary)).Response.StatusCode);
        var (fresh, _) = await LoginAsync("sara.analyst", "Permanent#Pass1");
        Assert.False(fresh.GetProperty("mustChangePassword").GetBoolean());

        // Reset password → a new temporary password and the gate again; sessions revoked.
        var reset = await (await admin.PostAsync($"{Api}/admin/admin-users/{Str(user, "id")}/reset-password", null)).ReadJsonAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = Str(fresh, "refreshToken") })).StatusCode);
        var (again, _) = await LoginAsync("sara.analyst", Str(reset, "temporaryPassword"));
        Assert.True(again.GetProperty("mustChangePassword").GetBoolean());

        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "admin_account").Select(a => a.Action).ToListAsync());
        Assert.Contains("admin_user.create", audit);
        Assert.Contains("admin.password_changed", audit);
        Assert.Contains("admin_user.reset_password", audit);
        Assert.Contains("admin.login", audit);
    }

    [Fact]
    public async Task Last_super_admin_and_self_disable_are_protected_and_only_star_grants_super_admin()
    {
        using var admin = await fixture.LoginAdminAsync();
        var seedId = await fixture.Factory.WithDbAsync(db => db.AdminAccounts.Where(a => a.Username == "admin").Select(a => a.Id).FirstAsync());
        var superAdmin = await RoleIdAsync(admin, PermissionCatalog.SuperAdminRole);
        var usersRole = Str(await (await admin.PostAsJsonAsync($"{Api}/admin/roles", new
        {
            code = "user_managers", nameAr = "إدارة المستخدمين", nameEn = "User managers", permissionCodes = new[] { PermissionCatalog.AdminUsersManage, PermissionCatalog.AdminRolesManage },
        })).ReadJsonAsync(), "id");

        // Self-disable and removing the only super_admin.
        var self = await admin.PostAsync($"{Api}/admin/admin-users/{seedId}/disable", null);
        Assert.Equal(HttpStatusCode.Conflict, self.StatusCode);
        Assert.Equal("cannot_disable_self", (await self.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var demote = await admin.PutAsJsonAsync($"{Api}/admin/admin-users/{seedId}", new { fullName = "ATA Admin", roleIds = new[] { usersRole } });
        Assert.Equal(HttpStatusCode.Conflict, demote.StatusCode);
        Assert.Equal("last_super_admin", (await demote.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());

        // A user manager (no "*") cannot disable the last super admin, nor grant super_admin.
        var manager = await CreateUserAsync(admin, "user.manager", fixture.NextPhone(), usersRole);
        var (managerClient, _) = await ActivateAsync("user.manager", Str(manager, "temporaryPassword"));
        var disableLast = await managerClient.PostAsync($"{Api}/admin/admin-users/{seedId}/disable", null);
        Assert.Equal(HttpStatusCode.Conflict, disableLast.StatusCode);
        Assert.Equal("last_super_admin", (await disableLast.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var grant = await managerClient.PostAsJsonAsync($"{Api}/admin/admin-users", new { username = "sneaky", fullName = "x", phoneNumber = fixture.NextPhone(), roleIds = new[] { superAdmin } });
        Assert.Equal(HttpStatusCode.Forbidden, grant.StatusCode);
        Assert.Equal("*", (await grant.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("permission").GetString());

        // A super admin may grant super_admin; both holders are listed.
        var second = await CreateUserAsync(admin, "second.root", fixture.NextPhone(), superAdmin);
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"{Api}/admin/admin-users/{Str(second.GetProperty("adminUser"), "id")}", new { fullName = "Second Root", roleIds = new[] { superAdmin, usersRole } })).StatusCode);
        var list = await (await admin.GetAsync($"{Api}/admin/admin-users?roleId={superAdmin}&isActive=true")).ReadJsonAsync();
        Assert.Equal(2, list.GetProperty("total").GetInt32());
        var search = await (await admin.GetAsync($"{Api}/admin/admin-users?search=user.man")).ReadJsonAsync();
        Assert.Equal("user.manager", Str(search.GetProperty("items")[0], "username"));
    }

    [Fact]
    public async Task Disabling_or_changing_roles_revokes_refresh_tokens_and_blocks_login()
    {
        using var admin = await fixture.LoginAdminAsync();
        var analyst = await RoleIdAsync(admin, "analyst");
        var finance = await RoleIdAsync(admin, "finance");
        var created = await CreateUserAsync(admin, "omar.finance", fixture.NextPhone(), analyst);
        var id = Str(created.GetProperty("adminUser"), "id");
        await ActivateAsync("omar.finance", Str(created, "temporaryPassword"), "Omar#Finance123");
        using var anonymous = fixture.CreateClient();

        // Role change → every refresh token revoked; the new login carries the new permissions.
        var (auth1, _) = await LoginAsync("omar.finance", "Omar#Finance123");
        (await admin.PutAsJsonAsync($"{Api}/admin/admin-users/{id}", new { fullName = "Omar", roleIds = new[] { finance } })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = Str(auth1, "refreshToken") })).StatusCode);
        var (auth2, _) = await LoginAsync("omar.finance", "Omar#Finance123");
        Assert.Contains(PermissionCatalog.PayoutsApprove, auth2.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));

        // Refresh keeps an admin session (and re-derives the permissions) until the account is disabled.
        var refreshed = await (await anonymous.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = Str(auth2, "refreshToken") })).ReadJsonAsync();
        Assert.Contains(PermissionCatalog.PayoutsApprove, refreshed.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        var disabled = await (await admin.PostAsync($"{Api}/admin/admin-users/{id}/disable", null)).ReadJsonAsync();
        Assert.False(disabled.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken = Str(refreshed, "refreshToken") })).StatusCode);
        var blocked = (await LoginAsync("omar.finance", "Omar#Finance123")).Response;
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("account_suspended", await blocked.ErrorCodeAsync());
        var active = await fixture.Factory.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == Guid.Parse(Str(created.GetProperty("adminUser"), "userId")) && t.RevokedAt == null));
        Assert.Equal(0, active);

        (await admin.PostAsync($"{Api}/admin/admin-users/{id}/enable", null)).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync("omar.finance", "Omar#Finance123")).Response.StatusCode);

        // Detail with sessions, per-session and all-session revocation (dashboard additions).
        var detail = await (await admin.GetAsync($"{Api}/admin/admin-users/{id}")).ReadJsonAsync();
        Assert.Equal(1, detail.GetProperty("sessions").GetArrayLength());
        Assert.Contains(PermissionCatalog.PayoutsApprove, detail.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        var sessionId = Str(detail.GetProperty("sessions")[0], "id");
        Assert.Equal(1, (await (await admin.GetAsync($"{Api}/admin/admin-users/{id}/sessions")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Api}/admin/admin-users/{id}/sessions/{sessionId}")).StatusCode);
        Assert.Equal(0, (await (await admin.GetAsync($"{Api}/admin/admin-users/{id}/sessions")).ReadJsonAsync()).GetArrayLength());
        await LoginAsync("omar.finance", "Omar#Finance123");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"{Api}/admin/admin-users/{id}/revoke-sessions", null)).StatusCode);
        Assert.Equal(0, (await (await admin.GetAsync($"{Api}/admin/admin-users/{id}/sessions")).ReadJsonAsync()).GetArrayLength());

        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityId == Guid.Parse(id)).Select(a => a.Action).ToListAsync());
        Assert.Contains("admin_user.update", audit);
        Assert.Contains("admin_user.disable", audit);
        Assert.Contains("admin_user.enable", audit);
        Assert.Contains("admin_user.revoke_sessions", audit);
    }
}
