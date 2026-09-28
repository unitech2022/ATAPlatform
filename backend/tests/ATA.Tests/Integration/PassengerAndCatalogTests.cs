using System.Net;
using System.Net.Http.Json;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

public class PassengerAndCatalogTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Catalog_is_localized_by_accept_language()
    {
        using var ar = fixture.CreateClient(language: "ar");
        using var en = fixture.CreateClient(language: "en-US,en;q=0.9");

        var arCategories = await (await ar.GetAsync("/api/v1/catalog/ride-categories")).ReadJsonAsync();
        var enCategories = await (await en.GetAsync("/api/v1/catalog/ride-categories")).ReadJsonAsync();
        Assert.Equal(6, arCategories.GetArrayLength());
        Assert.Equal("اقتصادي", arCategories[1].GetProperty("name").GetString());
        Assert.Equal("Economy", enCategories[1].GetProperty("name").GetString());
        Assert.Equal(38m, enCategories[1].GetProperty("estimate").GetProperty("price").GetDecimal());

        var cities = await (await en.GetAsync("/api/v1/catalog/cities")).ReadJsonAsync();
        Assert.Equal("Riyadh", cities[0].GetProperty("name").GetString());

        var types = await (await ar.GetAsync("/api/v1/catalog/document-types")).ReadJsonAsync();
        Assert.Equal(5, types.GetArrayLength());
        Assert.Equal("driver", types[0].GetProperty("appliesTo").GetString());
    }

    [Fact]
    public async Task Saved_places_and_preferences_round_trip()
    {
        var (client, _) = await fixture.LoginAsync("passenger");

        var saved = await client.PutAsJsonAsync("/api/v1/passenger/saved-places/home", new { name = "المنزل", address = "حي النرجس", latitude = 24.83, longitude = 46.65 });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal("home", (await saved.ReadJsonAsync()).GetProperty("label").GetString());

        var bad = await client.PutAsJsonAsync("/api/v1/passenger/saved-places/garage", new { name = "x", address = "y", latitude = 1, longitude = 1 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);

        var places = await (await client.GetAsync("/api/v1/passenger/saved-places")).ReadJsonAsync();
        Assert.Single(places.EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/v1/passenger/saved-places/home")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/v1/passenger/saved-places/home")).StatusCode);

        var prefs = await client.PatchAsJsonAsync("/api/v1/passenger/preferences", new { defaultPaymentMethod = "wallet", preferFemaleDriver = true });
        Assert.Equal("wallet", (await prefs.ReadJsonAsync()).GetProperty("defaultPaymentMethod").GetString());

        var wallet = await (await client.GetAsync("/api/v1/wallet")).ReadJsonAsync();
        Assert.True(wallet.GetProperty("paymentMethods")[0].GetProperty("isDefault").GetBoolean());

        var trips = await (await client.GetAsync("/api/v1/passenger/trips?status=active")).ReadJsonAsync();
        Assert.Equal(0, trips.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Me_update_and_delete()
    {
        var (client, _) = await fixture.LoginAsync("passenger");
        var updated = await client.PatchAsJsonAsync("/api/v1/me", new { fullName = "أحمد", language = "en", gender = "male", acceptTerms = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var user = await updated.ReadJsonAsync();
        Assert.Equal("أحمد", user.GetProperty("fullName").GetString());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, user.GetProperty("termsAcceptedAt").ValueKind);

        var prefs = await client.PutAsJsonAsync("/api/v1/me/notification-preferences", new { trips = true, wallet = false, safety = true, offers = false });
        Assert.False((await prefs.ReadJsonAsync()).GetProperty("wallet").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/api/v1/me/devices", new { deviceId = "web-1", platform = "web", deviceName = "Chrome" })).StatusCode);

        Assert.Equal(HttpStatusCode.Accepted, (await client.DeleteAsync("/api/v1/me")).StatusCode);
        var afterDelete = await client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Forbidden, afterDelete.StatusCode);
        Assert.Equal("account_suspended", await afterDelete.ErrorCodeAsync());
    }

    [Fact]
    public async Task Health_and_openapi_are_exposed()
    {
        using var client = fixture.CreateClient();
        var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal("Healthy", (await health.ReadJsonAsync()).GetProperty("status").GetString());

        var openapi = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openapi.StatusCode);
        var doc = await openapi.ReadJsonAsync();
        Assert.True(doc.GetProperty("paths").TryGetProperty("/api/v1/auth/otp/request", out _));

        var unknown = await client.GetAsync("/api/v1/nope");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("not_found", await unknown.ErrorCodeAsync());
    }
}
