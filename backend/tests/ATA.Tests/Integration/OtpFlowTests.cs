using System.Net;
using System.Net.Http.Json;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

public class OtpFlowTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Request_then_verify_creates_passenger_and_returns_tokens()
    {
        var phone = fixture.NextPhone();
        using var client = fixture.CreateClient();

        var requested = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger", language = "ar" });
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        var otp = await requested.ReadJsonAsync();
        Assert.Equal("+966" + phone[1..], otp.GetProperty("phoneNumber").GetString());
        Assert.Equal(300, otp.GetProperty("expiresInSeconds").GetInt32());
        Assert.Equal(60, otp.GetProperty("resendAfterSeconds").GetInt32());
        var code = otp.GetProperty("devCode").GetString();
        Assert.Matches("^[0-9]{4}$", code);

        var verified = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new
        {
            requestId = otp.GetProperty("requestId").GetString(), phoneNumber = phone, code, role = "passenger",
            device = new { deviceId = "d1", platform = "ios", deviceName = "iPhone", appVersion = "1.0.0" },
        });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var auth = await verified.ReadJsonAsync();
        Assert.True(auth.GetProperty("isNewUser").GetBoolean());
        Assert.Equal("passenger", auth.GetProperty("user").GetProperty("roles")[0].GetString());
        Assert.Equal(3600, auth.GetProperty("accessTokenExpiresIn").GetInt32());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, auth.GetProperty("driver").ValueKind);

        using var me = fixture.CreateClient(auth.GetProperty("accessToken").GetString());
        var meResponse = await me.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var meJson = await meResponse.ReadJsonAsync();
        Assert.Equal("cash", meJson.GetProperty("passenger").GetProperty("defaultPaymentMethod").GetString());
    }

    [Fact]
    public async Task Driver_login_creates_draft_application_with_number()
    {
        var (_, auth) = await fixture.LoginAsync("driver");
        var driver = auth.GetProperty("driver");
        Assert.Matches("^ATA-[0-9]{5}$", driver.GetProperty("applicationNumber").GetString());
        Assert.Equal("draft", driver.GetProperty("applicationStatus").GetString());
    }

    [Fact]
    public async Task Wrong_code_reports_attempts_left_then_locks()
    {
        var phone = fixture.NextPhone();
        using var client = fixture.CreateClient();
        var otp = await (await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" })).ReadJsonAsync();
        var requestId = otp.GetProperty("requestId").GetString();
        var wrong = otp.GetProperty("devCode").GetString() == "0000" ? "1111" : "0000";

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId, phoneNumber = phone, code = wrong, role = "passenger" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = (await response.ReadJsonAsync()).GetProperty("error");
            Assert.Equal("otp_invalid", error.GetProperty("code").GetString());
            Assert.Equal(5 - attempt, error.GetProperty("details").GetProperty("attemptsLeft").GetInt32());
        }

        var locked = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId, phoneNumber = phone, code = wrong, role = "passenger" });
        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.Equal("otp_locked", await locked.ErrorCodeAsync());

        // Even the right code is refused once locked.
        var correct = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new { requestId, phoneNumber = phone, code = otp.GetProperty("devCode").GetString(), role = "passenger" });
        Assert.Equal("otp_locked", await correct.ErrorCodeAsync());
    }

    [Fact]
    public async Task Expired_code_is_rejected()
    {
        var phone = fixture.NextPhone();
        using var client = fixture.CreateClient();
        var otp = await (await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" })).ReadJsonAsync();

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(6));
        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new
        {
            requestId = otp.GetProperty("requestId").GetString(), phoneNumber = phone, code = otp.GetProperty("devCode").GetString(), role = "passenger",
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("otp_expired", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Resend_cooldown_and_per_phone_quota_return_429()
    {
        var phone = fixture.NextPhone();
        using var client = fixture.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" })).StatusCode);

        var tooSoon = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" });
        Assert.Equal(HttpStatusCode.TooManyRequests, tooSoon.StatusCode);
        var error = (await tooSoon.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("rate_limited", error.GetProperty("code").GetString());
        Assert.InRange(error.GetProperty("details").GetProperty("retryAfterSeconds").GetInt32(), 1, 60);

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" })).StatusCode);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(61));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" })).StatusCode);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(61));

        var quota = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role = "passenger" });
        Assert.Equal(HttpStatusCode.TooManyRequests, quota.StatusCode);
        Assert.Equal("rate_limited", await quota.ErrorCodeAsync());
        Assert.True(quota.Headers.RetryAfter is not null);
    }

    [Fact]
    public async Task Invalid_phone_returns_phone_invalid_localized_in_english()
    {
        using var client = fixture.CreateClient(language: "en");
        var response = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = "0412345678", role = "passenger" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("phone_invalid", error.GetProperty("code").GetString());
        Assert.Equal("The phone number is invalid", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_is_revoked()
    {
        var (_, auth) = await fixture.LoginAsync("passenger");
        var refreshToken = auth.GetProperty("refreshToken").GetString();
        using var client = fixture.CreateClient();

        var rotated = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, rotated.StatusCode);
        var next = await rotated.ReadJsonAsync();
        Assert.NotEqual(refreshToken, next.GetProperty("refreshToken").GetString());

        var reused = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
        Assert.Equal("unauthorized", await reused.ErrorCodeAsync());

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = next.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_without_token_returns_error_envelope()
    {
        using var client = fixture.CreateClient();
        var response = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("unauthorized", await response.ErrorCodeAsync());

        var (passenger, _) = await fixture.LoginAsync("passenger");
        var forbidden = await passenger.GetAsync("/api/v1/admin/dashboard/summary");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal("forbidden", await forbidden.ErrorCodeAsync());
    }
}
