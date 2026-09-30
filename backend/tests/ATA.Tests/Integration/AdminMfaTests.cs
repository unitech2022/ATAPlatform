using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Security;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>MFA mandatory (<c>Admin:MfaRequired=true</c>) with the production admin access lifetime.</summary>
public sealed class MfaFixture() : ApiFixture(new Dictionary<string, string?> { ["Admin:MfaRequired"] = "true", ["Admin:AccessTokenMinutes"] = "15" });

/// <summary>F20 TOTP (doc 12 §F20.4 / §F20.5): enrolment, verification with replay guard and ±1 step, recovery codes, MFA and password lockouts.</summary>
public sealed class AdminMfaTests(MfaFixture fixture) : IClassFixture<MfaFixture>
{
    private const string Api = "/api/v1";

    private static string Str(JsonElement e, string name) => e.GetProperty(name).GetString()!;

    private FakeClock Clock => fixture.Factory.Clock;

    private string CodeAt(string secret, int stepOffset = 0) => Totp.Code(secret, Totp.StepAt(Clock.UtcNow) + stepOffset);

    private async Task<HttpResponseMessage> PostAsync(string path, object body)
    {
        var response = await fixture.CreateClient().PostAsJsonAsync($"{Api}{path}", body);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private static async Task<(string Code, JsonElement Details)> ErrorAsync(HttpResponseMessage response)
    {
        var error = (await response.ReadJsonAsync()).GetProperty("error");
        return (error.GetProperty("code").GetString()!, error.GetProperty("details"));
    }

    private async Task<JsonElement> LoginAsync(string username, string password)
    {
        var response = await PostAsync("/auth/admin/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    /// <summary>Creates the account, enrols TOTP at the first login and returns the secret, recovery codes and the session.</summary>
    private async Task<(string Secret, List<string> RecoveryCodes, JsonElement Auth)> EnrolledAsync(string username, string password, params string[] permissions)
    {
        await TestAdmins.EnsureAsync(fixture, username, password, permissions);
        var challenge = await LoginAsync(username, password);
        var token = Str(challenge, "mfaToken");
        var secret = Str(await (await PostAsync("/auth/admin/mfa/enroll", new { mfaToken = token })).ReadJsonAsync(), "secret");
        var confirmed = await (await PostAsync("/auth/admin/mfa/enroll/confirm", new { mfaToken = token, code = CodeAt(secret) })).ReadJsonAsync();
        Clock.Advance(TimeSpan.FromSeconds(Totp.PeriodSeconds));
        return (secret, confirmed.GetProperty("recoveryCodes").EnumerateArray().Select(c => c.GetString()!).ToList(), confirmed.GetProperty("auth"));
    }

    [Fact]
    public void Totp_matches_the_rfc_6238_sha1_test_vectors()
    {
        var secret = Totp.Base32Encode("12345678901234567890"u8);
        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", secret);
        Assert.Equal("287082", Totp.Code(secret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime)));
        Assert.Equal("081804", Totp.Code(secret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime)));
        Assert.Equal("050471", Totp.Code(secret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(1111111111).UtcDateTime)));
        Assert.Equal("005924", Totp.Code(secret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(1234567890).UtcDateTime)));
        Assert.Equal(Totp.SecretBytes, Totp.Base32Decode(Totp.NewSecret()).Length);
    }

    [Fact]
    public async Task Enrolment_then_verification_with_replay_guard_one_step_window_and_expiring_mfa_token()
    {
        await TestAdmins.EnsureAsync(fixture, "mfa.one", "MfaOne#Pass123", [PermissionCatalog.TripsView]);
        var challenge = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.True(challenge.GetProperty("mfaEnrollmentRequired").GetBoolean());
        Assert.False(challenge.TryGetProperty("accessToken", out _));
        var token = Str(challenge, "mfaToken");

        var enrollment = await (await PostAsync("/auth/admin/mfa/enroll", new { mfaToken = token })).ReadJsonAsync();
        var secret = Str(enrollment, "secret");
        Assert.Matches("^[A-Z2-7]{32}$", secret);
        Assert.Equal($"otpauth://totp/ATA%20Admin:mfa.one?secret={secret}&issuer=ATA%20Admin&digits=6&period=30", Str(enrollment, "otpauthUri"));
        var stored = await fixture.Factory.WithDbAsync(db => db.AdminAccounts.FirstAsync(a => a.Username == "mfa.one"));
        Assert.NotNull(stored.MfaSecret);
        Assert.DoesNotContain(secret, stored.MfaSecret);
        Assert.False(stored.MfaEnabled);

        var wrong = await PostAsync("/auth/admin/mfa/enroll/confirm", new { mfaToken = token, code = CodeAt(secret, 5) });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        var (wrongCode, wrongDetails) = await ErrorAsync(wrong);
        Assert.Equal("mfa_invalid", wrongCode);
        Assert.Equal(4, wrongDetails.GetProperty("attemptsLeft").GetInt32());

        var enrolCode = CodeAt(secret);
        var confirmed = await PostAsync("/auth/admin/mfa/enroll/confirm", new { mfaToken = token, code = enrolCode });
        Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
        var body = await confirmed.ReadJsonAsync();
        var codes = body.GetProperty("recoveryCodes").EnumerateArray().Select(c => c.GetString()!).ToList();
        Assert.Equal(10, codes.Distinct().Count());
        Assert.All(codes, c => Assert.Matches("^[a-z2-7]{4}-[a-z2-7]{4}$", c));
        Assert.False(string.IsNullOrEmpty(Str(body.GetProperty("auth"), "accessToken")));
        Assert.Equal(900, body.GetProperty("auth").GetProperty("accessTokenExpiresIn").GetInt32());
        var hashes = await fixture.Factory.WithDbAsync(db => db.AdminRecoveryCodes.Where(c => c.AdminAccountId == stored.Id).Select(c => c.CodeHash).ToListAsync());
        Assert.Equal(10, hashes.Count);
        Assert.DoesNotContain(codes[0], hashes);

        // The code used at enrolment cannot be replayed at the next login (same step).
        var login = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.True(login.GetProperty("mfaRequired").GetBoolean());
        Assert.Equal(["totp", "recovery_code"], login.GetProperty("methods").EnumerateArray().Select(m => m.GetString()));
        var replay = await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(login, "mfaToken"), code = enrolCode });
        Assert.Equal("mfa_invalid", await replay.ErrorCodeAsync());

        Clock.Advance(TimeSpan.FromSeconds(30));
        var next = CodeAt(secret);
        var verified = await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(login, "mfaToken"), code = next });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var auth = await verified.ReadJsonAsync();
        Assert.Equal(10, auth.GetProperty("recoveryCodesRemaining").GetInt32());
        Assert.Equal([PermissionCatalog.TripsView], auth.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));
        var again = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.Equal("mfa_invalid", await (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(again, "mfaToken"), code = next })).ErrorCodeAsync());

        // ±1 step: the previous and the next step are accepted, two steps back is not.
        Clock.Advance(TimeSpan.FromSeconds(90));
        var previousStep = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.Equal(HttpStatusCode.OK, (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(previousStep, "mfaToken"), code = CodeAt(secret, -1) })).StatusCode);
        var nextStep = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.Equal(HttpStatusCode.OK, (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(nextStep, "mfaToken"), code = CodeAt(secret, 1) })).StatusCode);
        Clock.Advance(TimeSpan.FromSeconds(120));
        var tooOld = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.Equal("mfa_invalid", await (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(tooOld, "mfaToken"), code = CodeAt(secret, -2) })).ErrorCodeAsync());

        // The mfaToken expires after Admin:MfaTokenMinutes and belongs to its stage.
        var stale = await LoginAsync("mfa.one", "MfaOne#Pass123");
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync("/auth/admin/mfa/enroll", new { mfaToken = Str(stale, "mfaToken") })).StatusCode);
        Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(stale, "mfaToken"), code = CodeAt(secret) })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = "garbage", code = CodeAt(secret) })).StatusCode);

        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityId == stored.Id).Select(a => a.Action).ToListAsync());
        Assert.Contains("admin.mfa_enrolled", audit);
        Assert.Contains("admin.mfa_failed", audit);
        Assert.Contains("admin.login", audit);
    }

    [Fact]
    public async Task Recovery_codes_work_once_and_can_be_regenerated_with_a_totp_code()
    {
        var (secret, codes, auth) = await EnrolledAsync("mfa.two", "MfaTwo#Pass123", PermissionCatalog.TripsView);
        var login = await LoginAsync("mfa.two", "MfaTwo#Pass123");
        var loose = codes[0].Replace("-", string.Empty).ToUpperInvariant();
        var used = await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(login, "mfaToken"), recoveryCode = loose });
        Assert.Equal(HttpStatusCode.OK, used.StatusCode);
        Assert.Equal(9, (await used.ReadJsonAsync()).GetProperty("recoveryCodesRemaining").GetInt32());
        var reuse = await LoginAsync("mfa.two", "MfaTwo#Pass123");
        Assert.Equal("mfa_invalid", await (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(reuse, "mfaToken"), recoveryCode = codes[0] })).ErrorCodeAsync());

        using var client = fixture.CreateClient(Str(auth, "accessToken"));
        var bad = await client.PostAsJsonAsync($"{Api}/admin/me/mfa/recovery-codes", new { code = CodeAt(secret, 7) });
        Assert.Equal("mfa_invalid", await bad.ErrorCodeAsync());
        var regenerated = await (await client.PostAsJsonAsync($"{Api}/admin/me/mfa/recovery-codes", new { code = CodeAt(secret) })).ReadJsonAsync();
        var fresh = regenerated.GetProperty("recoveryCodes").EnumerateArray().Select(c => c.GetString()!).ToList();
        Assert.Equal(10, fresh.Count);
        var old = await LoginAsync("mfa.two", "MfaTwo#Pass123");
        Assert.Equal("mfa_invalid", await (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(old, "mfaToken"), recoveryCode = codes[1] })).ErrorCodeAsync());
        var me = await (await client.GetAsync($"{Api}/admin/me")).ReadJsonAsync();
        Assert.True(me.GetProperty("mfaEnabled").GetBoolean());
        var enrolAgain = await client.PostAsync($"{Api}/admin/me/mfa/enroll", null);
        Assert.Equal(HttpStatusCode.Conflict, enrolAgain.StatusCode);
    }

    [Fact]
    public async Task Five_wrong_codes_lock_the_mfa_step_until_a_new_password_login()
    {
        var (secret, _, _) = await EnrolledAsync("mfa.three", "MfaThree#Pass1", PermissionCatalog.TripsView);
        var login = await LoginAsync("mfa.three", "MfaThree#Pass1");
        var token = Str(login, "mfaToken");
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var wrong = await PostAsync("/auth/admin/mfa/verify", new { mfaToken = token, code = CodeAt(secret, 9) });
            Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
            Assert.Equal(5 - attempt, (await wrong.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("attemptsLeft").GetInt32());
        }

        var fifth = await PostAsync("/auth/admin/mfa/verify", new { mfaToken = token, code = CodeAt(secret, 9) });
        Assert.Equal(HttpStatusCode.TooManyRequests, fifth.StatusCode);
        Assert.Equal("mfa_locked", await fifth.ErrorCodeAsync());
        Assert.Equal("mfa_locked", await (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = token, code = CodeAt(secret) })).ErrorCodeAsync());

        var relogin = await LoginAsync("mfa.three", "MfaThree#Pass1");
        Assert.Equal(HttpStatusCode.OK, (await PostAsync("/auth/admin/mfa/verify", new { mfaToken = Str(relogin, "mfaToken"), code = CodeAt(secret) })).StatusCode);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_with_retry_after_until_the_lockout_ends()
    {
        await TestAdmins.EnsureAsync(fixture, "lock.me", "LockMe#Pass123", [PermissionCatalog.TripsView]);
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var wrong = await PostAsync("/auth/admin/login", new { username = "lock.me", password = "Nope#Nope1234" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
            Assert.Equal("invalid_credentials", await wrong.ErrorCodeAsync());
        }

        var locked = await PostAsync("/auth/admin/login", new { username = "lock.me", password = "Nope#Nope1234" });
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        var (lockedCode, lockedDetails) = await ErrorAsync(locked);
        Assert.Equal("account_locked", lockedCode);
        Assert.Equal(900, lockedDetails.GetProperty("retryAfterSeconds").GetInt32());
        Assert.Equal("900", locked.Headers.GetValues("Retry-After").Single());

        Clock.Advance(TimeSpan.FromMinutes(5));
        var (stillCode, stillDetails) = await ErrorAsync(await PostAsync("/auth/admin/login", new { username = "lock.me", password = "LockMe#Pass123" }));
        Assert.Equal("account_locked", stillCode);
        Assert.Equal(600, stillDetails.GetProperty("retryAfterSeconds").GetInt32());

        Clock.Advance(TimeSpan.FromMinutes(10));
        var open = await LoginAsync("lock.me", "LockMe#Pass123");
        Assert.True(open.GetProperty("mfaEnrollmentRequired").GetBoolean());
        var failures = await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "admin.login_failed" && a.AfterJson!.Contains("lock.me")));
        Assert.Equal(5, failures);
    }

    [Fact]
    public async Task Reset_mfa_and_unlock_by_an_admin()
    {
        var (_, _, rootAuth) = await EnrolledAsync("root.mfa", "RootMfa#Pass12", PermissionCatalog.All);
        using var root = fixture.CreateClient(Str(rootAuth, "accessToken"));
        await EnrolledAsync("mfa.four", "MfaFour#Pass123", PermissionCatalog.TripsView);
        var id = await fixture.Factory.WithDbAsync(db => db.AdminAccounts.Where(a => a.Username == "mfa.four").Select(a => a.Id).FirstAsync());

        var reset = await (await root.PostAsync($"{Api}/admin/admin-users/{id}/reset-mfa", null)).ReadJsonAsync();
        Assert.False(reset.GetProperty("mfaEnabled").GetBoolean());
        Assert.Equal(0, await fixture.Factory.WithDbAsync(db => db.AdminRecoveryCodes.CountAsync(c => c.AdminAccountId == id)));
        Assert.True((await LoginAsync("mfa.four", "MfaFour#Pass123")).GetProperty("mfaEnrollmentRequired").GetBoolean());

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await PostAsync("/auth/admin/login", new { username = "mfa.four", password = "Wrong#Wrong123" });
        }

        var listed = await (await root.GetAsync($"{Api}/admin/admin-users?search=mfa.four")).ReadJsonAsync();
        Assert.NotEqual(JsonValueKind.Null, listed.GetProperty("items")[0].GetProperty("lockedUntil").ValueKind);
        var unlocked = await (await root.PostAsync($"{Api}/admin/admin-users/{id}/unlock", null)).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, unlocked.GetProperty("lockedUntil").ValueKind);
        Assert.True((await LoginAsync("mfa.four", "MfaFour#Pass123")).GetProperty("mfaEnrollmentRequired").GetBoolean());
    }

    [Fact]
    public async Task A_signed_in_admin_without_mfa_can_enrol_from_the_account_page()
    {
        // A signed-in session whose MFA was switched off (e.g. by a reset) enrols again from /admin/me.
        var (_, _, rootAuth) = await EnrolledAsync("root.self", "RootSelf#Pass1", PermissionCatalog.All);
        using var root = fixture.CreateClient(Str(rootAuth, "accessToken"));
        var id = await fixture.Factory.WithDbAsync(db => db.AdminAccounts.Where(a => a.Username == "root.self").Select(a => a.Id).FirstAsync());
        await fixture.Factory.WithDbAsync(async db =>
        {
            var account = await db.AdminAccounts.FirstAsync(a => a.Id == id);
            account.MfaEnabled = false;
            account.MfaSecret = null;
            await db.SaveChangesAsync();
            return true;
        });

        var enrollment = await (await root.PostAsync($"{Api}/admin/me/mfa/enroll", null)).ReadJsonAsync();
        var secret = Str(enrollment, "secret");
        var confirmed = await (await root.PostAsJsonAsync($"{Api}/admin/me/mfa/enroll/confirm", new { code = CodeAt(secret) })).ReadJsonAsync();
        Assert.Equal(10, confirmed.GetProperty("recoveryCodes").GetArrayLength());
        Assert.True((await LoginAsync("root.self", "RootSelf#Pass1")).GetProperty("mfaRequired").GetBoolean());
    }
}
