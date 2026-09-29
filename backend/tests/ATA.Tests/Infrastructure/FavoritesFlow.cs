using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Infrastructure;

/// <summary>API host with a favourites limit of one (F16 <c>422 favorites_limit</c>).</summary>
public sealed class FavoriteLimitFixture() : ApiFixture(new Dictionary<string, string?> { ["Favorites:MaxPerPassenger"] = "1" });

/// <summary>Shared steps for the F16 tests: a passenger who completed a ride and saved the driver, favourite requests, discount rules, offer helpers.</summary>
public static class FavoritesFlow
{
    public static object Request((decimal Lat, decimal Lng) area, Guid favoriteDriverId, string paymentMethod = "cash", string pricingMode = "fixed", decimal? offeredPrice = null,
        string? quoteId = null, string? promoCode = null, string bookingType = "now", DateTime? scheduledAt = null) =>
        RewardsFlow.Request(area, promoCode, paymentMethod, quoteId, pricingMode, offeredPrice, null, bookingType, favoriteDriverId, scheduledAt);

    /// <summary>A passenger and an online driver who completed one ride together, the driver saved as a favourite (by driver id).</summary>
    public static async Task<(SafetyFlow.Party Passenger, SafetyFlow.Party Driver, Guid DriverId, string FirstTripId)> PassengerWithFavoriteAsync(
        ApiFixture fixture, (decimal Lat, decimal Lng) area, string driverName = "محمد العتيبي")
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area, driverName);
        var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        await AddFavoriteAsync(passenger.Client, driverId);
        return (passenger, driver, driverId, tripId);
    }

    public static async Task<JsonElement> AddFavoriteAsync(HttpClient passenger, Guid driverId)
    {
        var response = await passenger.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    /// <summary>Saves the driver without the completed-trip rule (drivers the test only needs as list entries).</summary>
    public static async Task SaveDirectlyAsync(ApiFixture fixture, Guid passengerUserId, Guid driverId) =>
        await fixture.Factory.WithDbAsync(async db =>
        {
            var passengerId = await db.Passengers.Where(p => p.UserId == passengerUserId).Select(p => p.Id).FirstAsync();
            db.FavoriteDrivers.Add(new ATA.Domain.Favorites.FavoriteDriver { PassengerId = passengerId, DriverId = driverId });
            await db.SaveChangesAsync();
            return true;
        });

    public static async Task<JsonElement> CreateRuleAsync(HttpClient admin, string name, Action<Dictionary<string, object?>>? configure = null, DateTime? now = null)
    {
        var at = now ?? new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        var body = new Dictionary<string, object?>
        {
            ["name"] = name, ["discountPercent"] = 10m, ["maxDiscountAmount"] = 10m, ["stackableWithPromotions"] = false, ["validFrom"] = at.AddDays(-1), ["validTo"] = null,
            ["priority"] = 0, ["isActive"] = true,
        };
        configure?.Invoke(body);
        var response = await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task DeleteRuleAsync(HttpClient admin, JsonElement rule) =>
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/favorite-discount-rules/{rule.GetProperty("id").GetString()}")).StatusCode);

    /// <summary>The favourite-driver discount a quote shows for the category (0 without a favourite line).</summary>
    public static decimal QuoteDiscount(JsonElement quote, string categoryCode = "economy")
    {
        var category = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == categoryCode);
        var line = category.GetProperty("breakdown").GetProperty("discounts").EnumerateArray().FirstOrDefault(l => l.GetProperty("source").GetString() == "favorite_driver");
        return line.ValueKind == JsonValueKind.Object ? line.GetProperty("amount").GetDecimal() : 0m;
    }

    /// <summary>The F10 fare before discount and rounding, from the stored fare breakdown of a completed trip.</summary>
    public static (decimal Core, decimal Base) FareOf(Trip trip)
    {
        var b = JsonDocument.Parse(trip.FareBreakdown!).RootElement;
        decimal D(string name) => b.GetProperty(name).GetDecimal();
        var core = D("baseFare") + D("distanceFare") + D("timeFare") + D("waitingFare") + D("minFareAdjustment");
        return (core, core + D("bookingFee") + D("serviceFee"));
    }

    public static async Task<JsonElement> ActiveOfferAsync(HttpClient driver) => await (await driver.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();

    public static async Task<JsonElement> TripAsync(HttpClient client, string tripId, string role = "passenger") =>
        await (await client.GetAsync($"/api/v1/{role}/trips/{tripId}")).ReadJsonAsync();

    /// <summary>Requests a favourite trip and returns its JSON (201 expected).</summary>
    public static async Task<JsonElement> RequestAsync(HttpClient passenger, object request)
    {
        var response = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task MoveDriverAsync(HttpClient driver, decimal lat, decimal lng) =>
        (await driver.PutAsJsonAsync("/api/v1/driver/location", new { lat, lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();

    /// <summary>An admin account holding only <paramref name="permissionsJson"/> (e.g. <c>["trips.view"]</c>).</summary>
    public static async Task<HttpClient> LimitedAdminAsync(ApiFixture fixture, string username, string permissionsJson)
    {
        await fixture.Factory.WithDbAsync(async db =>
        {
            if (!await db.AdminAccounts.AnyAsync(a => a.Username == username))
            {
                using var scope = fixture.Factory.Services.CreateScope();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var user = new User { PhoneNumber = $"+9665{Random.Shared.Next(10000000, 99999999)}", FullName = $"Admin {username}", PhoneVerifiedAt = DateTime.UtcNow };
                user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Admin });
                db.Users.Add(user);
                db.AdminAccounts.Add(new AdminAccount { UserId = user.Id, Username = username, PasswordHash = hasher.Hash("Limited@12345"), Permissions = permissionsJson });
                await db.SaveChangesAsync();
            }

            return true;
        });
        return await fixture.LoginAdminAsync(username, "Limited@12345");
    }
}
