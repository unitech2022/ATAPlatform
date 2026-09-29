using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Infrastructure;

/// <summary>Shared steps for the F15 tests: trip requests with a promo code, completed rides, admin-created promotions and incentives.</summary>
public static class RewardsFlow
{
    public static object Request((decimal Lat, decimal Lng) area, string? promoCode = null, string paymentMethod = "cash", string? quoteId = null, string pricingMode = "fixed",
        decimal? offeredPrice = null, Guid? rideCategoryId = null, string bookingType = "now") => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = rideCategoryId ?? SeedIds.RideCategories.Economy,
        bookingType,
        paymentMethod,
        pricingMode,
        offeredPrice,
        quoteId,
        promoCode,
    };

    public static object Quote((decimal Lat, decimal Lng) area, string? promoCode = null, Guid? rideCategoryId = null) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = rideCategoryId ?? SeedIds.RideCategories.Economy,
        bookingType = "now",
        promoCode,
    };

    /// <summary>Creates a promotion through the admin API (valid from yesterday for 30 days unless overridden) and returns its JSON.</summary>
    public static async Task<JsonElement> CreatePromotionAsync(HttpClient admin, string code, Action<Dictionary<string, object?>>? configure = null, DateTime? now = null)
    {
        var at = now ?? new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var body = new Dictionary<string, object?>
        {
            ["code"] = code, ["nameAr"] = $"عرض {code}", ["nameEn"] = $"Offer {code}", ["type"] = "fixed", ["value"] = 5m,
            ["validFrom"] = at.AddDays(-1), ["validTo"] = at.AddDays(30), ["perUserLimit"] = 5, ["isPublic"] = true, ["isActive"] = true,
        };
        configure?.Invoke(body);
        var response = await admin.PostAsJsonAsync("/api/v1/admin/promotions", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    /// <summary>Requests (with <paramref name="request"/>), assigns and completes a ride with an existing online driver; returns the trip id.</summary>
    public static async Task<string> CompleteRideAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, SafetyFlow.Party passenger, SafetyFlow.Party driver, object request,
        object? completion = null)
    {
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger.Client, driver.Client, request);
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver.Client, tripId, pin);
        var completed = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", completion ?? new { });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        return tripId;
    }

    public static async Task<ATA.Domain.Trips.Trip> TripAsync(ApiFixture fixture, string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.Trips.AsNoTracking().FirstAsync(t => t.Id == Guid.Parse(tripId)));

    /// <summary>Asserts the error code and optionally <c>details.scope</c> / <c>details.reason</c> (reads the body once).</summary>
    public static async Task AssertErrorAsync(HttpResponseMessage response, string code, string? scope = null, string? reason = null)
    {
        var error = (await response.ReadJsonAsync()).GetProperty("error");
        Assert.Equal(code, error.GetProperty("code").GetString());
        if (scope is not null) Assert.Equal(scope, error.GetProperty("details").GetProperty("scope").GetString());
        if (reason is not null) Assert.Equal(reason, error.GetProperty("details").GetProperty("reason").GetString());
    }

    public static async Task<string> ErrorReasonAsync(HttpResponseMessage response) =>
        (await response.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString()!;
}
