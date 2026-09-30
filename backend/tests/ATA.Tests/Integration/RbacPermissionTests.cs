using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Integration;

/// <summary>F20 permission catalogue (doc 12 §F20.2 / §F20.3): startup sync, the route table and the 403 of every admin endpoint.</summary>
public sealed partial class RbacPermissionTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Api = "/api/v1";

    private IReadOnlyList<RouteEndpoint> AdminEndpoints() =>
        fixture.Factory.Services.GetServices<EndpointDataSource>().SelectMany(s => s.Endpoints).OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText is { } raw && raw.StartsWith("/api/v1/admin", StringComparison.Ordinal))
            .DistinctBy(e => (e.RoutePattern.RawText, string.Join(',', e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()?.HttpMethods ?? [])))
            .ToList();

    [Fact]
    public async Task Startup_sync_mirrors_the_catalogue_seeds_system_roles_and_makes_the_seed_admin_super_admin()
    {
        await fixture.Factory.WithDbAsync(async db =>
        {
            // An obsolete permission (and its grants) disappears; a renamed one is fixed; re-running is idempotent.
            var legacy = new Permission { Code = "legacy.removed", Module = "legacy", NameAr = "x", NameEn = "x" };
            db.Permissions.Add(legacy);
            var analyst = await db.Roles.FirstAsync(r => r.Code == "analyst");
            db.RolePermissions.Add(new RolePermission { RoleId = analyst.Id, PermissionId = legacy.Id });
            (await db.Permissions.FirstAsync(p => p.Code == PermissionCatalog.TripsView)).NameEn = "Renamed";
            await db.SaveChangesAsync();
            return true;
        });

        for (var run = 0; run < 2; run++)
        {
            using var scope = fixture.Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<RbacSynchronizer>().SyncAsync();
        }

        await fixture.Factory.WithDbAsync(async db =>
        {
            var codes = await db.Permissions.OrderBy(p => p.SortOrder).Select(p => p.Code).ToListAsync();
            Assert.Equal(PermissionCatalog.Definitions.Select(d => d.Code), codes);
            Assert.Equal("View trips", (await db.Permissions.FirstAsync(p => p.Code == PermissionCatalog.TripsView)).NameEn);
            var roles = await db.Roles.Where(r => r.IsSystem).Select(r => r.Code).ToListAsync();
            Assert.Equal(PermissionCatalog.SystemRoles.Select(r => r.Code).OrderBy(c => c), roles.OrderBy(c => c));
            Assert.False(await db.RolePermissions.AnyAsync(rp => !db.Permissions.Any(p => p.Id == rp.PermissionId)));
            Assert.Equal(1, await db.Roles.CountAsync(r => r.Code == PermissionCatalog.SuperAdminRole));
            var seed = await db.AdminAccounts.FirstAsync(a => a.Username == "admin");
            var seedRoles = await (from ar in db.AdminAccountRoles join r in db.Roles on ar.RoleId equals r.Id where ar.AdminAccountId == seed.Id select r.Code).ToListAsync();
            Assert.Equal([PermissionCatalog.SuperAdminRole], seedRoles);
            Assert.Equal("[]", seed.Permissions);
            return true;
        });

        // The JWT perm claim and /admin/me derive from the roles.
        using var anonymous = fixture.CreateClient();
        var login = await (await anonymous.PostAsJsonAsync($"{Api}/auth/admin/login", new { username = "admin", password = "Admin@12345" })).ReadJsonAsync();
        Assert.Equal(["*"], login.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.False(login.GetProperty("mustChangePassword").GetBoolean());
        using var admin = fixture.CreateClient(login.GetProperty("accessToken").GetString());
        var me = await (await admin.GetAsync($"{Api}/admin/me")).ReadJsonAsync();
        Assert.Equal("admin", me.GetProperty("username").GetString());
        Assert.Equal(["*"], me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(PermissionCatalog.SuperAdminRole, me.GetProperty("roles")[0].GetProperty("code").GetString());
        Assert.False(me.GetProperty("mfaEnabled").GetBoolean());
    }

    [Fact]
    public void Every_admin_route_requires_a_catalogue_permission_or_is_explicitly_open_to_any_admin()
    {
        var endpoints = AdminEndpoints();
        Assert.True(endpoints.Count > 250, $"only {endpoints.Count} admin endpoints found");
        var missing = endpoints.Where(e => e.Metadata.GetOrderedMetadata<RequiredPermissionMetadata>().Count == 0 && e.Metadata.GetMetadata<AnyAdminMetadata>() is null)
            .Select(e => e.RoutePattern.RawText).ToList();
        Assert.True(missing.Count == 0, "admin routes without a permission: " + string.Join(", ", missing));
        var unknown = endpoints.SelectMany(e => e.Metadata.GetOrderedMetadata<RequiredPermissionMetadata>().SelectMany(m => m.AnyOf))
            .Where(code => !PermissionCatalog.Codes.Contains(code)).Distinct().ToList();
        Assert.True(unknown.Count == 0, "permissions missing from the catalogue: " + string.Join(", ", unknown));

        // Open to any admin: only /admin/me* and reading ride categories.
        var open = endpoints.Where(e => e.Metadata.GetOrderedMetadata<RequiredPermissionMetadata>().Count == 0).Select(e => e.RoutePattern.RawText!).ToList();
        Assert.All(open, raw => Assert.True(raw.StartsWith("/api/v1/admin/me", StringComparison.Ordinal) || raw == "/api/v1/admin/ride-categories/", raw));

        // Spot checks of the doc's mapping (§F20.2).
        string[] Required(string method, string raw) => endpoints.Single(e => e.RoutePattern.RawText == raw
                && (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) ?? false))
            .Metadata.GetOrderedMetadata<RequiredPermissionMetadata>().SelectMany(m => m.AnyOf).ToArray();
        Assert.Equal([PermissionCatalog.DashboardView], Required("GET", "/api/v1/admin/dashboard/summary"));
        Assert.Equal([PermissionCatalog.DriversView], Required("GET", "/api/v1/admin/drivers/"));
        Assert.Equal([PermissionCatalog.DriversReview], Required("POST", "/api/v1/admin/drivers/{id:guid}/approve"));
        Assert.Equal([PermissionCatalog.DriversReview], Required("POST", "/api/v1/admin/documents/{id:guid}/verify"));
        Assert.Equal([PermissionCatalog.PassengersView], Required("GET", "/api/v1/admin/passengers"));
        Assert.Equal([PermissionCatalog.UsersSuspend], Required("POST", "/api/v1/admin/users/{userId:guid}/suspend"));
        Assert.Equal([PermissionCatalog.CatalogManage], Required("POST", "/api/v1/admin/ride-categories/"));
        Assert.Equal([PermissionCatalog.AuditView], Required("GET", "/api/v1/admin/audit-logs"));
        Assert.Equal([PermissionCatalog.TripsView], Required("GET", "/api/v1/admin/trips/{id:guid}/matching"));
        Assert.Equal([PermissionCatalog.TripsCancel], Required("POST", "/api/v1/admin/trips/{id:guid}/cancel"));
        Assert.Equal([PermissionCatalog.LiveView], Required("GET", "/api/v1/admin/live"));
        Assert.Equal([PermissionCatalog.PricingView], Required("GET", "/api/v1/admin/zones/"));
        Assert.Equal([PermissionCatalog.PricingEdit], Required("POST", "/api/v1/admin/pricing/simulate"));
        Assert.Equal([PermissionCatalog.PricingView], Required("GET", "/api/v1/admin/demand/current"));
        Assert.Equal([PermissionCatalog.PricingView], Required("GET", "/api/v1/admin/matching-settings/"));
        Assert.Equal([PermissionCatalog.MatchingEdit], Required("PUT", "/api/v1/admin/matching-settings/{id:guid}"));
        Assert.Equal([PermissionCatalog.ReportsView], Required("GET", "/api/v1/admin/matching/stats"));
        Assert.Equal([PermissionCatalog.AdminUsersManage], Required("POST", "/api/v1/admin/admin-users/"));
        Assert.Equal([PermissionCatalog.AdminRolesManage], Required("GET", "/api/v1/admin/permissions"));
        Assert.Equal([PermissionCatalog.ReportsExport], Required("GET", "/api/v1/admin/reports/export"));
        Assert.Equal([PermissionCatalog.ReportsExport], Required("POST", "/api/v1/admin/reports/snapshots/rebuild"));
    }

    [Fact]
    public async Task Every_admin_endpoint_answers_403_with_the_missing_permission_for_a_role_without_it()
    {
        using var nobody = await TestAdmins.LoginWithAsync(fixture, "no-permissions", "Nobody@12345");
        var checkedCount = 0;
        foreach (var endpoint in AdminEndpoints())
        {
            var requirements = endpoint.Metadata.GetOrderedMetadata<RequiredPermissionMetadata>();
            var method = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.First();
            var path = RouteParameter().Replace(endpoint.RoutePattern.RawText!, m => m.Value.StartsWith("{code", StringComparison.Ordinal) ? "completed_trips" : Guid.NewGuid().ToString());
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (method != "GET")
            {
                var multipart = endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Http.Metadata.IAcceptsMetadata>()?.ContentTypes.Contains("multipart/form-data") == true;
                request.Content = multipart ? new MultipartFormDataContent { { new ByteArrayContent([1]), "file", "x.csv" } } : JsonContent.Create(new { });
            }

            var response = await nobody.SendAsync(request);
            if (requirements.Count == 0)
            {
                Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
                continue;
            }

            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{method} {path} → {(int)response.StatusCode}");
            var error = (await response.ReadJsonAsync()).GetProperty("error");
            Assert.Equal("forbidden", error.GetProperty("code").GetString());
            var details = error.GetProperty("details");
            var first = requirements[0].AnyOf;
            if (first.Count == 1)
            {
                Assert.Equal(first[0], details.GetProperty("permission").GetString());
            }
            else
            {
                Assert.Equal(first, details.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!));
            }

            checkedCount++;
        }

        Assert.True(checkedCount > 250);
    }

    [Fact]
    public async Task A_role_with_a_subset_of_permissions_reaches_only_its_endpoints_and_star_passes_everything()
    {
        using var viewer = await TestAdmins.LoginWithAsync(fixture, "trips-viewer", "Viewer@12345", PermissionCatalog.TripsView, PermissionCatalog.PricingView);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/trips")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/zones")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/ride-categories")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/me")).StatusCode);
        var forbidden = await viewer.PostAsJsonAsync($"{Api}/admin/zones", new { code = "x" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(PermissionCatalog.PricingEdit, (await forbidden.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("permission").GetString());
        var me = await (await viewer.GetAsync($"{Api}/admin/me")).ReadJsonAsync();
        Assert.Equal([PermissionCatalog.TripsView, PermissionCatalog.PricingView], me.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));

        using var admin = await fixture.LoginAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Api}/admin/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Api}/admin/admin-users")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"{Api}/admin/roles")).StatusCode);

        // A passenger never reaches /admin (policy), whatever its claims.
        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync($"{Api}/admin/me")).StatusCode);
    }

    [Fact]
    public async Task Permissions_list_is_the_catalogue_localised()
    {
        using var admin = await fixture.LoginAdminAsync();
        var list = await (await admin.GetAsync($"{Api}/admin/permissions")).ReadJsonAsync();
        Assert.Equal(PermissionCatalog.Definitions.Count, list.GetArrayLength());
        var first = list[0];
        Assert.Equal(PermissionCatalog.DashboardView, first.GetProperty("code").GetString());
        Assert.Equal("dashboard", first.GetProperty("module").GetString());
        Assert.Equal("عرض لوحة المعلومات", first.GetProperty("name").GetString());
        using var english = fixture.CreateClient(language: "en");
        english.DefaultRequestHeaders.Authorization = admin.DefaultRequestHeaders.Authorization;
        Assert.Equal("View dashboard", (await (await english.GetAsync($"{Api}/admin/permissions")).ReadJsonAsync())[0].GetProperty("name").GetString());
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RouteParameter();
}
