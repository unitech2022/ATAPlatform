using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Identity;
using ATA.Domain.Rbac;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Production admin session limits: access 15 min, idle 30 min, absolute 12 h.</summary>
public sealed class AdminSessionFixture() : ApiFixture(new Dictionary<string, string?> { ["Admin:AccessTokenMinutes"] = "15" });

/// <summary>F20 admin sessions (doc 12 §F20.4 / §F20.5): expiry rules, rotation, the session list and the cleanup job.</summary>
public sealed class AdminSessionTests(AdminSessionFixture fixture) : IClassFixture<AdminSessionFixture>
{
    private const string Api = "/api/v1";

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()!;

    private FakeClock Clock => fixture.Factory.Clock;

    private async Task<JsonElement> LoginAsync(string username, string password, string userAgent = "TestBrowser/1.0")
    {
        using var anonymous = fixture.CreateClient();
        anonymous.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        var response = await anonymous.PostAsJsonAsync($"{Api}/auth/admin/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private async Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        using var anonymous = fixture.CreateClient();
        var response = await anonymous.PostAsJsonAsync($"{Api}/auth/refresh", new { refreshToken });
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    [Fact]
    public async Task Access_expires_after_15_minutes_and_refresh_keeps_the_session_start_until_idle_or_absolute_limits()
    {
        await TestAdmins.EnsureAsync(fixture, "session.one", "Session#One123", [PermissionCatalog.TripsView]);
        var login = await LoginAsync("session.one", "Session#One123");
        Assert.Equal(900, login.GetProperty("accessTokenExpiresIn").GetInt32());
        var started = Clock.UtcNow;
        using (var client = fixture.CreateClient(Str(login, "accessToken")))
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{Api}/admin/me")).StatusCode);
            Clock.Advance(TimeSpan.FromMinutes(16));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Api}/admin/me")).StatusCode);
        }

        // Refreshing within the idle limit rotates the token; the session keeps its start time and user agent.
        var refreshed = await (await RefreshAsync(Str(login, "refreshToken"))).ReadJsonAsync();
        Assert.Equal([PermissionCatalog.TripsView], refreshed.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        using var current = fixture.CreateClient(Str(refreshed, "accessToken"));
        var sessions = await (await current.GetAsync($"{Api}/admin/me/sessions")).ReadJsonAsync();
        var session = Assert.Single(sessions.EnumerateArray());
        Assert.True(session.GetProperty("current").GetBoolean());
        Assert.Equal("TestBrowser/1.0", Str(session, "userAgent"));
        Assert.Equal(started, session.GetProperty("createdAt").GetDateTime().ToUniversalTime());
        Assert.Equal(Clock.UtcNow, session.GetProperty("lastUsedAt").GetDateTime().ToUniversalTime());
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Str(login, "refreshToken"))).StatusCode);

        // Keep refreshing every 25 minutes: allowed until 12 hours after the login, then refused.
        var token = Str(refreshed, "refreshToken");
        while (Clock.UtcNow.AddMinutes(25) < started.AddHours(12))
        {
            Clock.Advance(TimeSpan.FromMinutes(25));
            var next = await RefreshAsync(token);
            Assert.Equal(HttpStatusCode.OK, next.StatusCode);
            token = Str(await next.ReadJsonAsync(), "refreshToken");
        }

        Clock.Set(started.AddHours(12).AddMinutes(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(token)).StatusCode);
    }

    [Fact]
    public async Task Idle_sessions_are_refused_and_revoked_by_the_cleanup_job()
    {
        await TestAdmins.EnsureAsync(fixture, "session.two", "Session#Two123", [PermissionCatalog.TripsView]);
        var idle = await LoginAsync("session.two", "Session#Two123");
        Clock.Advance(TimeSpan.FromMinutes(31));
        var refused = await RefreshAsync(Str(idle, "refreshToken"));
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);

        var forgotten = await LoginAsync("session.two", "Session#Two123");
        var active = await LoginAsync("session.two", "Session#Two123");
        Clock.Advance(TimeSpan.FromMinutes(20));
        (await RefreshAsync(Str(active, "refreshToken"))).EnsureSuccessStatusCode();
        Clock.Advance(TimeSpan.FromMinutes(15));
        // "forgotten" has been idle 35 minutes; the rotated "active" session 15 minutes.
        Assert.True(await fixture.Factory.RunAdminSessionCleanupAsync() >= 1);
        var userId = await fixture.Factory.WithDbAsync(db => db.AdminAccounts.Where(a => a.Username == "session.two").Select(a => a.UserId).FirstAsync());
        var open = await fixture.Factory.WithDbAsync(db => db.RefreshTokens.CountAsync(t => t.UserId == userId && t.SessionKind == SessionKind.Admin && t.RevokedAt == null));
        Assert.Equal(1, open);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Str(forgotten, "refreshToken"))).StatusCode);
        Assert.Equal(0, await fixture.Factory.RunAdminSessionCleanupAsync());
    }

    [Fact]
    public async Task Own_sessions_can_be_listed_and_revoked_one_by_one_or_all_others()
    {
        await TestAdmins.EnsureAsync(fixture, "session.three", "Session#Three1", [PermissionCatalog.TripsView]);
        var laptop = await LoginAsync("session.three", "Session#Three1", "Laptop/1.0");
        var phone = await LoginAsync("session.three", "Session#Three1", "Phone/1.0");
        var tablet = await LoginAsync("session.three", "Session#Three1", "Tablet/1.0");
        using var client = fixture.CreateClient(Str(laptop, "accessToken"));
        var sessions = (await (await client.GetAsync($"{Api}/admin/me/sessions")).ReadJsonAsync()).EnumerateArray().ToList();
        Assert.Equal(3, sessions.Count);
        Assert.Equal("Laptop/1.0", Str(sessions.Single(s => s.GetProperty("current").GetBoolean()), "userAgent"));

        var phoneSession = Str(sessions.Single(s => Str(s, "userAgent") == "Phone/1.0"), "id");
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"{Api}/admin/me/sessions/{phoneSession}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Str(phone, "refreshToken"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"{Api}/admin/me/sessions/{Guid.NewGuid()}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"{Api}/admin/me/sessions/revoke-others", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(Str(tablet, "refreshToken"))).StatusCode);
        var left = (await (await client.GetAsync($"{Api}/admin/me/sessions")).ReadJsonAsync()).EnumerateArray().ToList();
        Assert.True(Assert.Single(left).GetProperty("current").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(Str(laptop, "refreshToken"))).StatusCode);
    }

    [Fact]
    public async Task App_and_corporate_sessions_keep_their_own_rules()
    {
        // An app (OTP) session of a user is not an admin session: no admin permissions are added on refresh.
        var (_, auth) = await fixture.LoginAsync("passenger");
        Clock.Advance(TimeSpan.FromMinutes(45));
        var refreshed = await (await RefreshAsync(Str(auth, "refreshToken"))).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, refreshed.GetProperty("permissions").ValueKind);
        Assert.False(refreshed.TryGetProperty("mustChangePassword", out _));
    }
}
