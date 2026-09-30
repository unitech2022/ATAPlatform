using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Infrastructure;

/// <summary>API host whose access tokens outlive the long fake-clock jumps of the scheduled-ride timelines (T − 24 h … T + 10 min).</summary>
public sealed class SchedulingFixture() : ApiFixture(new Dictionary<string, string?> { ["Jwt:AccessTokenMinutes"] = "100000" });

/// <summary>Shared steps for the F17 tests: scheduled bookings, drivers with reservations, the timeline jobs and direct database reads.</summary>
public static class SchedulingFlow
{
    /// <summary>A scheduled trip request from <paramref name="area"/> (economy, cash unless changed).</summary>
    public static object Request((decimal Lat, decimal Lng) area, DateTime scheduledAt, string paymentMethod = "cash", Guid? favoriteDriverId = null, Guid? rideCategoryId = null,
        Guid? airportPickupZoneId = null, string? airportTerminalCode = null, string? flightNumber = null, decimal? pickupLatOverride = null, decimal? pickupLngOverride = null,
        decimal dropoffDelta = 0.05m, string? quoteId = null, string? paymentMethodId = null, bool? preferFemaleDriver = null, bool scheduled = true,
        decimal? dropoffLatOverride = null, decimal? dropoffLngOverride = null) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = pickupLatOverride ?? area.Lat, lng = pickupLngOverride ?? area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = dropoffLatOverride ?? area.Lat + dropoffDelta, lng = dropoffLngOverride ?? area.Lng + dropoffDelta },
        stops = Array.Empty<object>(),
        rideCategoryId = rideCategoryId ?? SeedIds.RideCategories.Economy,
        bookingType = scheduled ? "scheduled" : "now",
        scheduledAt = scheduled ? scheduledAt : (DateTime?)null,
        paymentMethod,
        paymentMethodId,
        pricingMode = "fixed",
        preferFemaleDriver,
        favoriteDriverId,
        airportPickupZoneId,
        airportTerminalCode,
        flightNumber,
        quoteId,
    };

    /// <summary>The airport (King Khalid, seeded) reference points used by the airport tests.</summary>
    public static class Ruh
    {
        public static readonly (decimal Lat, decimal Lng) Terminal = (24.9600m, 46.6980m);
        /// <summary>Inside the seeded driver waiting area polygon.</summary>
        public static readonly (decimal Lat, decimal Lng) WaitingArea = (24.9355m, 46.6790m);
        /// <summary>Inside the airport geofence, outside the waiting area.</summary>
        public static readonly (decimal Lat, decimal Lng) Outside = (24.5000m, 46.5000m);
    }

    public static async Task<JsonElement> BookAsync(HttpClient passenger, (decimal Lat, decimal Lng) area, DateTime scheduledAt, string paymentMethod = "cash", Guid? favoriteDriverId = null,
        Guid? rideCategoryId = null)
    {
        var response = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", Request(area, scheduledAt, paymentMethod, favoriteDriverId, rideCategoryId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, object? body = null) => await client.PostAsJsonAsync(url, body ?? new { });

    public static async Task<JsonElement> ReserveAsync(HttpClient driver, string tripId)
    {
        var response = await driver.PostAsync($"/api/v1/driver/scheduled/{tripId}/reserve", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<JsonElement> ConfirmAsync(HttpClient driver, string tripId)
    {
        var response = await driver.PostAsync($"/api/v1/driver/scheduled/{tripId}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static async Task<JsonElement> MarketplaceAsync(HttpClient driver, (decimal Lat, decimal Lng)? at = null, string query = "")
    {
        var position = at is null ? string.Empty : $"lat={at.Value.Lat}&lng={at.Value.Lng}";
        var response = await driver.GetAsync($"/api/v1/driver/scheduled/marketplace?{position}{(position.Length > 0 && query.Length > 0 ? "&" : "")}{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    public static bool Lists(JsonElement page, string tripId) => page.GetProperty("items").EnumerateArray().Any(i => i.GetProperty("tripId").GetString() == tripId);

    public static async Task<JsonElement> PassengerTripAsync(HttpClient passenger, string tripId) =>
        await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();

    public static async Task<Trip> TripRowAsync(ApiFixture fixture, string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.Trips.AsNoTracking().FirstAsync(t => t.Id == Guid.Parse(tripId)));

    /// <summary>The newest reservation of the trip (any status).</summary>
    public static async Task<ScheduledRideReservation> ReservationAsync(ApiFixture fixture, string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.ScheduledRideReservations.AsNoTracking().Where(r => r.TripId == Guid.Parse(tripId)).OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).FirstAsync());

    public static async Task<List<ScheduledRideReservation>> ReservationsAsync(ApiFixture fixture, string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.ScheduledRideReservations.AsNoTracking().Where(r => r.TripId == Guid.Parse(tripId)).OrderBy(r => r.CreatedAt).ToListAsync());

    public static async Task<List<ScheduledRideReminder>> RemindersAsync(ApiFixture fixture, string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.ScheduledRideReminders.AsNoTracking().Where(r => r.TripId == Guid.Parse(tripId)).OrderBy(r => r.SendAt).ToListAsync());

    public static async Task<List<Notification>> NotificationsAsync(ApiFixture fixture, Guid userId, string type) =>
        await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().Where(n => n.UserId == userId && n.Type == type).OrderBy(n => n.CreatedAt).ToListAsync());

    public static async Task<List<TripEvent>> EventsAsync(ApiFixture fixture, string tripId, string? type = null) =>
        await fixture.Factory.WithDbAsync(db => db.TripEvents.AsNoTracking().Where(e => e.TripId == Guid.Parse(tripId) && (type == null || e.Type == type)).OrderBy(e => e.CreatedAt).ToListAsync());

    /// <summary>Moves the fake clock to <paramref name="instant"/> and runs the worker (and optionally the reminder job) once.</summary>
    public static async Task<int> AtAsync(ApiFixture fixture, DateTime instant, bool reminders = false)
    {
        fixture.Factory.Clock.Set(instant);
        var changed = await fixture.Factory.RunScheduledWorkerAsync();
        if (reminders)
        {
            changed += await fixture.Factory.RunScheduledRemindersAsync();
        }

        return changed;
    }

    /// <summary>An approved online driver (economy) of <paramref name="area"/>.</summary>
    public static Task<(SafetyFlow.Party Driver, Guid DriverId)> DriverAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, string name = "محمد العتيبي") =>
        SafetyFlow.OnlineDriverAsync(fixture, area, name);
}
