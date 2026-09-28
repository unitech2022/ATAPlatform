using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ATA.Tests.Infrastructure;

/// <summary>One API host per test class with helpers for the OTP login flow.</summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private int _phoneCounter = 100;

    public AtaWebApplicationFactory Factory { get; } = new();

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public async Task InitializeAsync() => await Factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    public HttpClient CreateClient(string? accessToken = null, string language = "ar")
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(language);
        if (accessToken is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return client;
    }

    public string NextPhone() => $"05{Interlocked.Increment(ref _phoneCounter):D8}";

    /// <summary>Runs request + verify for a fresh phone number and returns the authenticated client and auth payload.</summary>
    public async Task<(HttpClient Client, JsonElement Auth)> LoginAsync(string role, string? phone = null)
    {
        phone ??= NextPhone();
        using var anonymous = CreateClient();
        var requested = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/request", new { phoneNumber = phone, role, language = "ar" });
        requested.EnsureSuccessStatusCode();
        var otp = await requested.Content.ReadFromJsonAsync<JsonElement>();
        var verified = await anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new
        {
            requestId = otp.GetProperty("requestId").GetString(),
            phoneNumber = phone,
            code = otp.GetProperty("devCode").GetString(),
            role,
            device = new { deviceId = "test-device", platform = "android", deviceName = "Test", appVersion = "1.0.0" },
        });
        verified.EnsureSuccessStatusCode();
        var auth = await verified.Content.ReadFromJsonAsync<JsonElement>();
        return (CreateClient(auth.GetProperty("accessToken").GetString()), auth);
    }

    public async Task<HttpClient> LoginAdminAsync()
    {
        using var anonymous = CreateClient();
        var response = await anonymous.PostAsJsonAsync("/api/v1/auth/admin/login", new { username = "admin", password = "Admin@12345" });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<JsonElement>();
        return CreateClient(auth.GetProperty("accessToken").GetString());
    }
}

public static class HttpExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(ApiFixture.Json);

    public static async Task<string> ErrorCodeAsync(this HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("error").GetProperty("code").GetString()!;
}
