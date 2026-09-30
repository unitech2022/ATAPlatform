using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Marketplace visibility, reservation limits, overlap and the atomic claim (doc 11 §F17.3 steps 3–4).</summary>
public class ScheduledMarketplaceTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Only_one_driver_can_hold_a_trip_the_others_get_reservation_taken()
    {
        // The claim is one atomic `UPDATE trips SET reserved_driver_id … WHERE reserved_driver_id IS NULL`; the in-memory SQLite host shares a single connection, so true
        // parallelism is exercised in the MySQL smoke test and here the second and third drivers arrive after the first claim.
        var area = TripFlow.Area(0);
        var at = fixture.Factory.Clock.UtcNow.AddHours(4);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        var drivers = new List<SafetyFlow.Party>();
        for (var i = 0; i < 3; i++)
        {
            drivers.Add((await SchedulingFlow.DriverAsync(fixture, area, $"سائق {i}")).Driver);
        }

        await SchedulingFlow.ReserveAsync(drivers[0].Client, tripId);
        foreach (var loser in drivers.Skip(1))
        {
            var response = await loser.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/reserve", null);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("reservation_taken", await response.ErrorCodeAsync());
        }

        var reservations = await SchedulingFlow.ReservationsAsync(fixture, tripId);
        Assert.Equal(ReservationStatus.Reserved, Assert.Single(reservations).Status);
        Assert.Equal(reservations[0].DriverId, (await SchedulingFlow.TripRowAsync(fixture, tripId)).ReservedDriverId);
    }

    [Fact]
    public async Task Overlapping_windows_conflict_and_the_reservation_limit_is_enforced_per_rule()
    {
        var area = TripFlow.Area(1);
        var now = fixture.Factory.Clock.UtcNow;
        using var admin = await fixture.LoginAdminAsync();
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        async Task<string> Book(TimeSpan after) => (await SchedulingFlow.BookAsync((await SafetyFlow.PassengerAsync(fixture)).Client, area, now + after)).GetProperty("id").GetString()!;
        var first = await Book(TimeSpan.FromHours(3));
        var overlapping = await Book(TimeSpan.FromHours(3.5));
        var later = await Book(TimeSpan.FromHours(6));
        var latest = await Book(TimeSpan.FromHours(9));

        await SchedulingFlow.ReserveAsync(driver.Client, first);
        var conflict = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{overlapping}/reserve", null);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        var error = (await conflict.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("reservation_conflict", error.GetProperty("code").GetString());
        Assert.Equal(first, error.GetProperty("details").GetProperty("conflictingTripId").GetString());
        Assert.Null((await SchedulingFlow.TripRowAsync(fixture, overlapping)).ReservedDriverId);
        await SchedulingFlow.ReserveAsync(driver.Client, later);

        // Lower the limit to two active reservations through the rule; the third is refused with the maximum.
        var rules = await (await admin.GetAsync("/api/v1/admin/scheduled-ride-rules")).ReadJsonAsync();
        var global = rules.EnumerateArray().Single(r => r.GetProperty("cityId").ValueKind == JsonValueKind.Null && r.GetProperty("rideCategoryId").ValueKind == JsonValueKind.Null);
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(global.GetRawText())!;
        body.Remove("id"); body.Remove("createdAt"); body.Remove("updatedAt");
        body["maxReservationsPerDriver"] = JsonSerializer.SerializeToElement(2);
        var ruleUrl = $"/api/v1/admin/scheduled-ride-rules/{global.GetProperty("id").GetString()}";
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();
        var limited = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{latest}/reserve", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, limited.StatusCode);
        var limit = (await limited.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("reservation_limit_reached", limit.GetProperty("code").GetString());
        Assert.Equal(2, limit.GetProperty("details").GetProperty("max").GetInt32());
        body["maxReservationsPerDriver"] = JsonSerializer.SerializeToElement(5);
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();
        await SchedulingFlow.ReserveAsync(driver.Client, latest);
    }

    [Fact]
    public async Task Marketplace_hides_incompatible_far_restricted_reserved_and_female_only_trips_and_filters_by_date()
    {
        var area = TripFlow.Area(2);
        var now = fixture.Factory.Clock.UtcNow;
        var (driver, driverId) = await SchedulingFlow.DriverAsync(fixture, area);
        var (rival, _) = await SchedulingFlow.DriverAsync(fixture, area, "بندر");
        async Task<HttpClient> Rider() => (await SafetyFlow.PassengerAsync(fixture)).Client;

        var near = (await SchedulingFlow.BookAsync(await Rider(), area, now.AddHours(3))).GetProperty("id").GetString()!;
        var later = (await SchedulingFlow.BookAsync(await Rider(), area, now.AddHours(8))).GetProperty("id").GetString()!;
        var comfort = (await SchedulingFlow.BookAsync(await Rider(), area, now.AddHours(3), rideCategoryId: SeedIds.RideCategories.Comfort)).GetProperty("id").GetString()!;
        var far = (await (await (await Rider()).PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(3), pickupLatOverride: area.Lat + 0.36m))).ReadJsonAsync()).GetProperty("id").GetString()!;
        var femaleOnly = (await (await (await Rider()).PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(3), preferFemaleDriver: true))).ReadJsonAsync()).GetProperty("id").GetString()!;
        var taken = (await SchedulingFlow.BookAsync(await Rider(), area, now.AddHours(3))).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(rival.Client, taken);

        var market = await SchedulingFlow.MarketplaceAsync(driver.Client);
        Assert.True(SchedulingFlow.Lists(market, near));
        Assert.True(SchedulingFlow.Lists(market, later));
        Assert.False(SchedulingFlow.Lists(market, comfort));
        Assert.False(SchedulingFlow.Lists(market, far));
        Assert.False(SchedulingFlow.Lists(market, femaleOnly));
        Assert.False(SchedulingFlow.Lists(market, taken));
        Assert.Equal(near, market.GetProperty("items").EnumerateArray().First().GetProperty("tripId").GetString());

        // A trip 40 km away is out of the 30 km radius from the driver's position but listed from a position close to it.
        Assert.True(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(driver.Client, (area.Lat + 0.36m, area.Lng)), far));
        var to = Uri.EscapeDataString(now.AddHours(5).ToString("O"));
        var window = await SchedulingFlow.MarketplaceAsync(driver.Client, null, $"to={to}");
        Assert.True(SchedulingFlow.Lists(window, near));
        Assert.False(SchedulingFlow.Lists(window, later));
        var from = Uri.EscapeDataString(now.AddHours(6).ToString("O"));
        var afterwards = await SchedulingFlow.MarketplaceAsync(driver.Client, null, $"from={from}");
        Assert.False(SchedulingFlow.Lists(afterwards, near));
        Assert.True(SchedulingFlow.Lists(afterwards, later));
        var paged = await SchedulingFlow.MarketplaceAsync(driver.Client, null, "pageSize=1&page=2");
        Assert.Equal(1, paged.GetProperty("items").GetArrayLength());
        Assert.True(paged.GetProperty("total").GetInt32() >= 2);

        // Reserving what the marketplace does not show is a 404; a driver without an approved account is refused; a restricted one too.
        Assert.Equal(HttpStatusCode.NotFound, (await driver.Client.PostAsync($"/api/v1/driver/scheduled/{comfort}/reserve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await driver.Client.PostAsync($"/api/v1/driver/scheduled/{femaleOnly}/reserve", null)).StatusCode);
        Assert.Equal("reservation_taken", await (await driver.Client.PostAsync($"/api/v1/driver/scheduled/{taken}/reserve", null)).ErrorCodeAsync());
        var (fresh, _) = await fixture.LoginAsync("driver");
        Assert.Equal("driver_not_approved", await (await fresh.PostAsync($"/api/v1/driver/scheduled/{near}/reserve", null)).ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await fresh.GetAsync("/api/v1/driver/scheduled/marketplace?lat=24.3&lng=46.6")).StatusCode);

        await fixture.Factory.WithDbAsync(async db =>
        {
            var userId = await db.Drivers.Where(d => d.Id == driverId).Select(d => d.UserId).FirstAsync();
            db.ReliabilityProfiles.Add(new ReliabilityProfile
            {
                UserId = userId, Role = Role.Driver, WindowDays = 30, RestrictionLevel = RestrictionLevel.TemporarilyRestricted, RestrictedUntil = now.AddDays(1), LastComputedAt = now,
            });
            await db.SaveChangesAsync();
            return true;
        });
        Assert.Empty((await SchedulingFlow.MarketplaceAsync(driver.Client)).GetProperty("items").EnumerateArray());
        var restricted = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{near}/reserve", null);
        Assert.Equal(HttpStatusCode.Forbidden, restricted.StatusCode);
        Assert.Equal("account_restricted", await restricted.ErrorCodeAsync());
    }
}
