using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Airports;
using ATA.Domain.Airports;
using ATA.Domain.Cancellation;
using ATA.Domain.Matching;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Reject action <c>remove</c> and a single queue offer per trip (doc 11 §F17.9 configuration).</summary>
public sealed class AirportRemoveFixture() : ApiFixture(new Dictionary<string, string?>
{
    ["Jwt:AccessTokenMinutes"] = "100000", ["Airport:RejectAction"] = "remove", ["Airport:QueueMaxOffers"] = "1",
});

public static class AirportFlow
{
    public static async Task<Guid> ZoneIdAsync(HttpClient client, string code)
    {
        var airports = await (await client.GetAsync("/api/v1/catalog/airports")).ReadJsonAsync();
        var zone = airports.EnumerateArray().Single(a => a.GetProperty("code").GetString() == "RUH").GetProperty("pickupZones").EnumerateArray().Single(z => z.GetProperty("code").GetString() == code);
        return Guid.Parse(zone.GetProperty("id").GetString()!);
    }

    /// <summary>An airport pickup from <paramref name="zoneId"/> to a city point of <paramref name="area"/>; immediate unless a time is given.</summary>
    public static object PickupRequest((decimal Lat, decimal Lng) area, Guid? zoneId, string? flightNumber = null, DateTime? scheduledAt = null, Guid? categoryId = null) =>
        SchedulingFlow.Request(area, scheduledAt ?? DateTime.UtcNow, "cash", rideCategoryId: categoryId, airportPickupZoneId: zoneId, flightNumber: flightNumber,
            pickupLatOverride: SchedulingFlow.Ruh.Terminal.Lat, pickupLngOverride: SchedulingFlow.Ruh.Terminal.Lng, scheduled: scheduledAt is not null);

    public static async Task<JsonElement> QueueAsync(HttpClient driver) => await (await driver.GetAsync("/api/v1/driver/airport-queue")).ReadJsonAsync();

    public static async Task<AirportQueueEntry> EntryAsync(ApiFixture fixture, Guid driverId) =>
        await fixture.Factory.WithDbAsync(db => db.AirportQueueEntries.AsNoTracking().Where(e => e.DriverId == driverId).OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).FirstAsync());
}

public class AirportTripTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Catalog_and_resolve_list_terminals_pickup_zones_and_instructions()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        using var anonymous = fixture.CreateClient(language: "en");
        var catalog = await (await anonymous.GetAsync("/api/v1/catalog/airports")).ReadJsonAsync();
        var ruh = Assert.Single(catalog.EnumerateArray());
        Assert.Equal("RUH", ruh.GetProperty("code").GetString());
        Assert.Equal("King Khalid International Airport", ruh.GetProperty("name").GetString());
        Assert.Equal(24.9576m, ruh.GetProperty("lat").GetDecimal());
        Assert.Equal(5, ruh.GetProperty("terminals").GetArrayLength());
        Assert.Equal("T1", ruh.GetProperty("terminals")[0].GetProperty("terminalCode").GetString());
        var zones = ruh.GetProperty("pickupZones");
        Assert.Equal(10, zones.GetArrayLength());
        var first = zones.EnumerateArray().Single(z => z.GetProperty("code").GetString() == "T1-P1");
        Assert.Equal("Pickup zone 1 - Terminal 1", first.GetProperty("name").GetString());
        Assert.Equal("T1", first.GetProperty("terminalCode").GetString());
        Assert.Equal(15, first.GetProperty("freeWaitingMinutes").GetInt32());
        Assert.Contains("Terminal 1", first.GetProperty("instructions").GetString());

        var inside = await (await passenger.Client.GetAsync($"/api/v1/passenger/airports/resolve?lat={SchedulingFlow.Ruh.Terminal.Lat}&lng={SchedulingFlow.Ruh.Terminal.Lng}")).ReadJsonAsync();
        Assert.Equal("RUH", inside.GetProperty("airport").GetProperty("code").GetString());
        Assert.True(inside.GetProperty("requiresPickupZone").GetBoolean());
        Assert.Equal(10, inside.GetProperty("pickupZones").GetArrayLength());
        Assert.Equal(5, inside.GetProperty("terminals").GetArrayLength());
        var outside = await passenger.Client.GetAsync("/api/v1/passenger/airports/resolve?lat=24.5&lng=46.5");
        Assert.Equal(HttpStatusCode.OK, outside.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await outside.ReadJsonAsync()).ValueKind);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await passenger.Client.GetAsync("/api/v1/passenger/airports/resolve")).StatusCode);
    }

    [Fact]
    public async Task Airport_pickup_needs_a_zone_replaces_the_pickup_stores_terminal_and_normalized_flight_number_and_fixes_the_waiting_policy()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T1-P1");
        var terminalZone = (await (await passenger.Client.GetAsync("/api/v1/catalog/airports")).ReadJsonAsync()).EnumerateArray().Single().GetProperty("terminals")[0].GetProperty("id").GetString();

        var missing = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        var error = (await missing.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("airport_pickup_zone_required", error.GetProperty("code").GetString());
        Assert.Equal(SeedIds.AirportRuh.ToString(), error.GetProperty("details").GetProperty("airportId").GetString());
        var notAPickupZone = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, Guid.Parse(terminalZone!)));
        Assert.Equal("unknown", (await notAPickupZone.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("airportPickupZoneId").GetString());
        foreach (var bad in new[] { "S", "SV12345", "SV-1020", "1020SV" })
        {
            var invalid = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId, bad));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            Assert.Equal("invalid", (await invalid.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("flightNumber").GetString());
        }

        var created = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId, " sv 1020a "));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trip = await created.ReadJsonAsync();
        Assert.Equal("searching", trip.GetProperty("status").GetString());
        Assert.Equal("منطقة الالتقاط 1 - صالة 1", trip.GetProperty("pickup").GetProperty("name").GetString());
        var pickup = trip.GetProperty("pickup");
        Assert.NotEqual(SchedulingFlow.Ruh.Terminal.Lat, pickup.GetProperty("lat").GetDecimal());
        var airport = trip.GetProperty("airport");
        Assert.Equal("RUH", airport.GetProperty("code").GetString());
        Assert.Equal("pickup", airport.GetProperty("direction").GetString());
        Assert.Equal("T1", airport.GetProperty("terminalCode").GetString());
        Assert.Equal("SV1020A", airport.GetProperty("flightNumber").GetString());
        Assert.Equal(15, airport.GetProperty("freeWaitingMinutes").GetInt32());
        Assert.Equal("منطقة الالتقاط 1 - صالة 1", airport.GetProperty("zoneName").GetString());
        var row = await SchedulingFlow.TripRowAsync(fixture, trip.GetProperty("id").GetString()!);
        Assert.Equal(SeedIds.AirportRuh, row.AirportId);
        Assert.Equal(AirportDirection.Pickup, row.AirportDirection);
        Assert.Equal(zoneId, row.AirportZoneId);
        Assert.Equal("T1", row.TerminalCode);
        Assert.Equal("SV1020A", row.FlightNumber);
        Assert.Equal(pickup.GetProperty("lat").GetDecimal(), row.PickupLat);
        var policy = WaitingPolicy.Parse(row.WaitingPolicy)!;
        Assert.Equal(15, policy.FreeMinutes);
        Assert.Equal(0.35m, policy.PerMinute);
        Assert.Contains("\"freeMinutes\":15", row.WaitingPolicy);
    }

    [Fact]
    public async Task Airport_dropoff_takes_an_optional_terminal_and_the_airport_category_is_only_for_airport_trips()
    {
        var area = TripFlow.Area(1);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var toAirport = (decimal.Round(SchedulingFlow.Ruh.Terminal.Lat, 4), decimal.Round(SchedulingFlow.Ruh.Terminal.Lng, 4));

        var ok = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, airportTerminalCode: "t3", flightNumber: "ey 101",
            dropoffLatOverride: toAirport.Item1, dropoffLngOverride: toAirport.Item2));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var body = await ok.ReadJsonAsync();
        Assert.Equal("dropoff", body.GetProperty("airport").GetProperty("direction").GetString());
        Assert.Equal("T3", body.GetProperty("airport").GetProperty("terminalCode").GetString());
        Assert.Equal("EY101", body.GetProperty("airport").GetProperty("flightNumber").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("airport").GetProperty("freeWaitingMinutes").ValueKind);
        Assert.Null(WaitingPolicy.Parse((await SchedulingFlow.TripRowAsync(fixture, body.GetProperty("id").GetString()!)).WaitingPolicy));
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{body.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();

        var unknownTerminal = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, airportTerminalCode: "T9",
            dropoffLatOverride: toAirport.Item1, dropoffLngOverride: toAirport.Item2));
        Assert.Equal("unknown", (await unknownTerminal.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("airportTerminalCode").GetString());

        // The airport category needs the pickup or the dropoff at an airport.
        var cityTrip = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, rideCategoryId: SeedIds.RideCategories.Airport));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cityTrip.StatusCode);
        Assert.Equal("airport_category_not_applicable", await cityTrip.ErrorCodeAsync());
        var cityQuote = await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, rideCategoryId: SeedIds.RideCategories.Airport));
        Assert.Equal("airport_category_not_applicable", await cityQuote.ErrorCodeAsync());
        var plainQuote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        Assert.DoesNotContain(plainQuote.GetProperty("categories").EnumerateArray(), c => c.GetProperty("code").GetString() == "airport");
        Assert.Equal(5, plainQuote.GetProperty("categories").GetArrayLength());
        var airportQuote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, dropoffLatOverride: toAirport.Item1, dropoffLngOverride: toAirport.Item2))).ReadJsonAsync();
        Assert.Contains(airportQuote.GetProperty("categories").EnumerateArray(), c => c.GetProperty("code").GetString() == "airport");
        var airportRide = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, rideCategoryId: SeedIds.RideCategories.Airport,
            dropoffLatOverride: toAirport.Item1, dropoffLngOverride: toAirport.Item2));
        Assert.Equal(HttpStatusCode.Created, airportRide.StatusCode);
    }

    [Fact]
    public async Task Airport_quote_prices_from_the_pickup_zone_and_a_non_airport_trip_ignores_the_flight_number()
    {
        var area = TripFlow.Area(2);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T2-P1");
        var quote = await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", AirportFlow.PickupRequest(area, zoneId, "SV1"));
        Assert.Equal(HttpStatusCode.OK, quote.StatusCode);
        // The quote without a zone is accepted too (the zone is only required for the trip request).
        Assert.Equal(HttpStatusCode.OK, (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", AirportFlow.PickupRequest(area, null))).StatusCode);
        var city = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, flightNumber: "sv 1"));
        Assert.Equal(HttpStatusCode.Created, city.StatusCode);
        var body = await city.ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("airport").ValueKind);
        Assert.Null((await SchedulingFlow.TripRowAsync(fixture, body.GetProperty("id").GetString()!)).FlightNumber);
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{body.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var badFlight = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, DateTime.UtcNow, scheduled: false, flightNumber: "!!"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badFlight.StatusCode);
    }
}

/// <summary>The airport waiting policy replaces the pricing rule in the free waiting time, the waiting fee, the F14 stages and the no-show wait (doc 11 §F17.7).</summary>
public class AirportWaitingPolicyTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Zone_waiting_policy_takes_precedence_for_free_minutes_fees_cancellation_stages_and_no_show()
    {
        var area = TripFlow.Area(3);
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, SchedulingFlow.Ruh.Terminal);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T1-P2");

        var trip = await SafetyFlow.PassengerAsync(fixture);
        var created = await (await trip.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.Equal(tripId, offer.GetProperty("tripId").GetString());
        Assert.Equal("RUH", offer.GetProperty("airport").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, offer.GetProperty("airport").GetProperty("flightNumber").ValueKind);
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var pin = (await SchedulingFlow.PassengerTripAsync(trip.Client, tripId)).GetProperty("pin").GetString()!;
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/en-route", null)).EnsureSuccessStatusCode();
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/arrived", null)).EnsureSuccessStatusCode();
        var arrivedEvent = (await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.WaitingStarted)).Single();
        Assert.Contains("\"freeWaitingMinutes\":15", arrivedEvent.Data);
        var arrivedNotice = (await SchedulingFlow.NotificationsAsync(fixture, trip.UserId, ATA.Domain.Notifications.NotificationTypes.TripDriverArrived)).Single();
        Assert.Contains("15", arrivedNotice.BodyEn);

        // 10 minutes after arriving: still inside the 15 free minutes (the rule's 3 minutes would already be over) → stage `arrived`, no-show not yet allowed.
        clock.Advance(TimeSpan.FromMinutes(10));
        var early = await (await trip.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("arrived", early.GetProperty("stage").GetString());
        var tooEarly = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/no-show", new { });
        Assert.Equal("no_show_too_early", await tooEarly.ErrorCodeAsync());
        clock.Advance(TimeSpan.FromMinutes(6));
        var afterFree = await (await trip.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("waiting", afterFree.GetProperty("stage").GetString());

        // PIN at 20 minutes: 5 minutes are billable (20 − 15), not 17 (20 − 3).
        clock.Advance(TimeSpan.FromMinutes(4));
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/verify-pin", new { pin })).EnsureSuccessStatusCode();
        Assert.Equal(300, (await SchedulingFlow.TripRowAsync(fixture, tripId)).WaitingSeconds);
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/start", null)).EnsureSuccessStatusCode();
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var done = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(1.75m, JsonDocument.Parse(done.FareBreakdown!).RootElement.GetProperty("waitingFare").GetDecimal());

        // A per-minute charge on the zone replaces the rule's 0.35 per minute.
        var zone = (await (await admin.GetAsync($"/api/v1/admin/airports/{SeedIds.AirportRuh}/zones")).ReadJsonAsync()).EnumerateArray().Single(z => z.GetProperty("id").GetString() == zoneId.ToString());
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(zone.GetRawText())!;
        foreach (var key in new[] { "id", "airportId", "createdAt", "updatedAt" }) body.Remove(key);
        body["waitingPerMinute"] = JsonSerializer.SerializeToElement(2.0m);
        body["freeWaitingMinutes"] = JsonSerializer.SerializeToElement(5);
        (await admin.PutAsJsonAsync($"/api/v1/admin/airports/{SeedIds.AirportRuh}/zones/{zoneId}", body)).EnsureSuccessStatusCode();
        var second = await (await trip.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId))).ReadJsonAsync();
        var secondId = second.GetProperty("id").GetString()!;
        Assert.Equal(5, second.GetProperty("airport").GetProperty("freeWaitingMinutes").GetInt32());
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = SchedulingFlow.Ruh.Terminal.Lat, lng = SchedulingFlow.Ruh.Terminal.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        var secondOffer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{secondOffer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var secondPin = (await SchedulingFlow.PassengerTripAsync(trip.Client, secondId)).GetProperty("pin").GetString()!;
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{secondId}/arrived", null)).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromMinutes(10));
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{secondId}/verify-pin", new { pin = secondPin })).EnsureSuccessStatusCode();
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{secondId}/start", null)).EnsureSuccessStatusCode();
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{secondId}/complete", new { })).EnsureSuccessStatusCode();
        var secondDone = await SchedulingFlow.TripRowAsync(fixture, secondId);
        Assert.Equal(300, secondDone.WaitingSeconds);
        Assert.Equal(10.00m, JsonDocument.Parse(secondDone.FareBreakdown!).RootElement.GetProperty("waitingFare").GetDecimal());
        Assert.NotNull(passenger);
    }
}

/// <summary>One fixture per queue scenario: the seeded airport has a single queue, so scenarios must not share drivers.</summary>
public class AirportQueueMembershipTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static readonly (decimal Lat, decimal Lng) Waiting = SchedulingFlow.Ruh.WaitingArea;

    [Fact]
    public async Task Drivers_enter_by_location_get_positions_leave_after_the_grace_period_and_can_join_or_leave_manually()
    {
        var clock = fixture.Factory.Clock;
        var (a, aId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "أحمد");
        var status = await AirportFlow.QueueAsync(a.Client);
        Assert.True(status.GetProperty("inQueue").GetBoolean());
        Assert.Equal("RUH", status.GetProperty("airport").GetProperty("code").GetString());
        Assert.Equal(1, status.GetProperty("position").GetInt32());
        Assert.Equal(1, status.GetProperty("total").GetInt32());
        Assert.Equal(JsonValueKind.Null, status.GetProperty("estimatedWaitMinutes").ValueKind);

        clock.Advance(TimeSpan.FromSeconds(5));
        var (b, bId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "بندر");
        var second = await AirportFlow.QueueAsync(b.Client);
        Assert.Equal(2, second.GetProperty("position").GetInt32());
        Assert.Equal(2, second.GetProperty("total").GetInt32());
        Assert.Equal(1, (await AirportFlow.QueueAsync(a.Client)).GetProperty("position").GetInt32());

        // A driver outside every waiting area is offered the airport but cannot join (and does not enter automatically).
        clock.Advance(TimeSpan.FromSeconds(5));
        var (c, cId) = await SchedulingFlow.DriverAsync(fixture, SchedulingFlow.Ruh.Terminal, "خالد");
        var outside = await AirportFlow.QueueAsync(c.Client);
        Assert.False(outside.GetProperty("inQueue").GetBoolean());
        Assert.Equal("RUH", outside.GetProperty("eligibleAirport").GetProperty("code").GetString());
        var refused = await c.Client.PostAsJsonAsync("/api/v1/driver/airport-queue/join", new { lat = SchedulingFlow.Ruh.Terminal.Lat, lng = SchedulingFlow.Ruh.Terminal.Lng });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("not_in_airport_waiting_area", await refused.ErrorCodeAsync());
        var joined = await c.Client.PostAsJsonAsync("/api/v1/driver/airport-queue/join", new { lat = Waiting.Lat, lng = Waiting.Lng });
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        Assert.Equal(3, (await joined.ReadJsonAsync()).GetProperty("position").GetInt32());
        Assert.Equal(3, (await (await c.Client.PostAsJsonAsync("/api/v1/driver/airport-queue/join", new { lat = Waiting.Lat, lng = Waiting.Lng })).ReadJsonAsync()).GetProperty("position").GetInt32());
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.AirportQueueEntries.CountAsync(e => e.DriverId == cId)));

        // Leaving frees the place; joining again goes to the back.
        Assert.Equal(HttpStatusCode.NoContent, (await c.Client.PostAsync("/api/v1/driver/airport-queue/leave", null)).StatusCode);
        Assert.False((await AirportFlow.QueueAsync(c.Client)).GetProperty("inQueue").GetBoolean());
        Assert.Equal(AirportQueueStatus.Left, (await AirportFlow.EntryAsync(fixture, cId)).Status);
        clock.Advance(TimeSpan.FromSeconds(1));
        (await c.Client.PostAsJsonAsync("/api/v1/driver/airport-queue/join", new { lat = Waiting.Lat, lng = Waiting.Lng })).EnsureSuccessStatusCode();

        // A leaves the waiting area: he stays for the 180 s grace period and then leaves (exited_area); B is offline and leaves too (offline).
        (await a.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = SchedulingFlow.Ruh.Outside.Lat, lng = SchedulingFlow.Ruh.Outside.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        (await b.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        (await b.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = Waiting.Lat, lng = Waiting.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromSeconds(165));
        (await c.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = Waiting.Lat, lng = Waiting.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        await fixture.Factory.RunAirportQueueJobAsync();
        Assert.True((await AirportFlow.QueueAsync(a.Client)).GetProperty("inQueue").GetBoolean());
        clock.Advance(TimeSpan.FromSeconds(10));
        (await c.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = Waiting.Lat, lng = Waiting.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        await fixture.Factory.RunAirportQueueJobAsync();
        var aEntry = await AirportFlow.EntryAsync(fixture, aId);
        Assert.Equal(AirportQueueStatus.Left, aEntry.Status);
        Assert.Equal(AirportQueueLeftReason.ExitedArea, aEntry.LeftReason);
        var bEntry = await AirportFlow.EntryAsync(fixture, bId);
        Assert.Equal(AirportQueueStatus.Left, bEntry.Status);
        Assert.Equal(AirportQueueLeftReason.Offline, bEntry.LeftReason);
        Assert.Equal(1, (await AirportFlow.QueueAsync(c.Client)).GetProperty("position").GetInt32());
    }
}

/// <summary>One fixture per queue scenario: the seeded airport has a single queue, so scenarios must not share drivers.</summary>
public class AirportQueueSignalRTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static readonly (decimal Lat, decimal Lng) Waiting = SchedulingFlow.Ruh.WaitingArea;

    [Fact]
    public async Task Queue_position_changes_are_pushed_to_the_driver_over_signalr()
    {
        var clock = fixture.Factory.Clock;
        var (a, aId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "سعد");
        await using var hub = TripFlow.Hub(fixture, a.Token);
        var updates = new System.Collections.Concurrent.ConcurrentQueue<JsonElement>();
        var arrived = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>("AirportQueueUpdated", u =>
        {
            updates.Enqueue(u);
            if (u.GetProperty("total").GetInt32() == 2)
            {
                arrived.TrySetResult(u);
            }
        });
        await hub.StartAsync();
        clock.Advance(TimeSpan.FromSeconds(5));
        var (b, _) = await SchedulingFlow.DriverAsync(fixture, Waiting, "ماجد");
        Assert.NotNull(b);
        await fixture.Factory.RunAirportQueueJobAsync();
        var pushed = await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(1, pushed.GetProperty("position").GetInt32());
        Assert.Equal(2, pushed.GetProperty("total").GetInt32());
        Assert.Equal(aId, (await AirportFlow.EntryAsync(fixture, aId)).DriverId);
    }
}

/// <summary>One fixture per queue scenario: the seeded airport has a single queue, so scenarios must not share drivers.</summary>
public class AirportQueueFifoOfferTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static readonly (decimal Lat, decimal Lng) Waiting = SchedulingFlow.Ruh.WaitingArea;

    [Fact]
    public async Task Offers_go_to_the_queue_in_fifo_order_a_reject_moves_the_driver_to_the_back_and_the_normal_search_takes_over()
    {
        var clock = fixture.Factory.Clock;
        var area = TripFlow.Area(4);
        var (a, aId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "أحمد");
        clock.Advance(TimeSpan.FromSeconds(5));
        var (b, bId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "بندر");
        // A closer, free driver that is not in the queue: without the queue he would win on score.
        var (near, nearId) = await SchedulingFlow.DriverAsync(fixture, SchedulingFlow.Ruh.Terminal, "قريب");
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T2-P1");
        var tripId = (await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId, "SV1020"))).ReadJsonAsync()).GetProperty("id").GetString()!;

        await fixture.Factory.RunMatcherAsync();
        var first = await FavoritesFlow.ActiveOfferAsync(a.Client);
        Assert.Equal(tripId, first.GetProperty("tripId").GetString());
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(b.Client)).ValueKind);
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(near.Client)).ValueKind);
        Assert.Equal(AirportQueueStatus.Offered, (await AirportFlow.EntryAsync(fixture, aId)).Status);
        using var admin = await fixture.LoginAdminAsync();
        var rounds = (await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync()).GetProperty("attempts");
        Assert.Equal("airport_queue", Assert.Single(rounds.EnumerateArray()).GetProperty("mode").GetString());

        // A rejects: back of the queue (entered_at = now) and B is offered next.
        clock.Advance(TimeSpan.FromSeconds(3));
        (await a.Client.PostAsJsonAsync($"/api/v1/driver/offers/{first.GetProperty("id").GetString()}/reject", new { reasonCode = "busy" })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        var aBack = await AirportFlow.EntryAsync(fixture, aId);
        Assert.Equal(AirportQueueStatus.Waiting, aBack.Status);
        Assert.Equal(fixture.Factory.Clock.UtcNow, aBack.EnteredAt);
        var second = await FavoritesFlow.ActiveOfferAsync(b.Client);
        Assert.Equal(tripId, second.GetProperty("tripId").GetString());
        Assert.Equal(2, (await AirportFlow.QueueAsync(a.Client)).GetProperty("total").GetInt32());

        // B lets the offer expire: back of the queue as well; nobody in the queue is left for this trip, so the normal search offers the closest free driver.
        clock.Advance(TimeSpan.FromSeconds(21));
        await FavoritesFlow.MoveDriverAsync(a.Client, Waiting.Lat, Waiting.Lng);
        await FavoritesFlow.MoveDriverAsync(b.Client, Waiting.Lat, Waiting.Lng);
        await FavoritesFlow.MoveDriverAsync(near.Client, SchedulingFlow.Ruh.Terminal.Lat, SchedulingFlow.Ruh.Terminal.Lng);
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(AirportQueueStatus.Waiting, (await AirportFlow.EntryAsync(fixture, bId)).Status);
        var normal = await FavoritesFlow.ActiveOfferAsync(near.Client);
        Assert.Equal(tripId, normal.GetProperty("tripId").GetString());
        var modes = (await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync()).GetProperty("attempts").EnumerateArray().Select(r => r.GetProperty("mode").GetString()).ToList();
        Assert.Equal(new[] { "airport_queue", "airport_queue", "normal" }, modes.ToArray());
        (await near.Client.PostAsync($"/api/v1/driver/offers/{normal.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        Assert.NotEqual(nearId, Guid.Empty);
        Assert.Contains((await (await admin.GetAsync($"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue")).ReadJsonAsync()).EnumerateArray(), e => e.GetProperty("driverName").GetString() == "بندر");
    }
}

/// <summary>One fixture per queue scenario: the seeded airport has a single queue, so scenarios must not share drivers.</summary>
public class AirportQueueDispatchTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static readonly (decimal Lat, decimal Lng) Waiting = SchedulingFlow.Ruh.WaitingArea;

    [Fact]
    public async Task A_queued_driver_who_accepts_is_dispatched_and_the_estimate_uses_the_dispatch_interval()
    {
        var clock = fixture.Factory.Clock;
        var area = TripFlow.Area(5);
        var drivers = new List<(SafetyFlow.Party Driver, Guid Id)>();
        for (var i = 0; i < 4; i++)
        {
            drivers.Add(await SchedulingFlow.DriverAsync(fixture, Waiting, $"سائق {i}"));
            clock.Advance(TimeSpan.FromSeconds(2));
        }

        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T3-P1");
        // Two dispatches ten minutes apart give an average interval of ten minutes.
        for (var round = 0; round < 2; round++)
        {
            var driver = drivers[round].Driver;
            var tripId = (await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId))).ReadJsonAsync()).GetProperty("id").GetString()!;
            foreach (var d in drivers) await FavoritesFlow.MoveDriverAsync(d.Driver.Client, Waiting.Lat, Waiting.Lng);
            await fixture.Factory.RunMatcherAsync();
            var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
            Assert.Equal(tripId, offer.GetProperty("tripId").GetString());
            (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
            var entry = await AirportFlow.EntryAsync(fixture, drivers[round].Id);
            Assert.Equal(AirportQueueStatus.Dispatched, entry.Status);
            Assert.Equal(AirportQueueLeftReason.TripAssigned, entry.LeftReason);
            var pin = (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("pin").GetString()!;
            await TripFlow.DriveAsync(driver.Client, tripId, pin);
            (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
            clock.Advance(TimeSpan.FromMinutes(10));
        }

        var third = await AirportFlow.QueueAsync(drivers[3].Driver.Client);
        Assert.Equal(2, third.GetProperty("position").GetInt32());
        Assert.Equal(20, third.GetProperty("estimatedWaitMinutes").GetInt32());
        // The first driver finished his ride, drove back into the waiting area and queued again at the back.
        Assert.Equal(3, third.GetProperty("total").GetInt32());
    }
}

/// <summary>One fixture per queue scenario: the seeded airport has a single queue, so scenarios must not share drivers.</summary>
public class AirportQueueAdminTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static readonly (decimal Lat, decimal Lng) Waiting = SchedulingFlow.Ruh.WaitingArea;

    [Fact]
    public async Task Admin_sees_the_live_queue_and_removes_an_entry_with_an_audited_reason()
    {
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var (a, aId) = await SchedulingFlow.DriverAsync(fixture, Waiting, "أحمد");
        clock.Advance(TimeSpan.FromSeconds(2));
        var (b, _) = await SchedulingFlow.DriverAsync(fixture, Waiting, "بندر");
        var queue = (await (await admin.GetAsync($"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue")).ReadJsonAsync()).EnumerateArray().Where(e => e.GetProperty("driverName").GetString() is "أحمد" or "بندر").ToList();
        Assert.Equal(new[] { "أحمد", "بندر" }, queue.Select(e => e.GetProperty("driverName").GetString()!).ToArray());
        Assert.Equal("economy", queue[0].GetProperty("categoryCode").GetString());
        Assert.Equal("waiting", queue[0].GetProperty("status").GetString());
        Assert.Equal(queue[0].GetProperty("position").GetInt32() + 1, queue[1].GetProperty("position").GetInt32());

        var entryId = queue[0].GetProperty("entryId").GetString();
        using var noReason = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue/{entryId}") { Content = JsonContent.Create(new { reason = new string('x', 501) }) };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.SendAsync(noReason)).StatusCode);
        using var remove = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue/{entryId}") { Content = JsonContent.Create(new { reason = "سلوك غير لائق" }) };
        Assert.Equal(HttpStatusCode.NoContent, (await admin.SendAsync(remove)).StatusCode);
        var entry = await AirportFlow.EntryAsync(fixture, aId);
        Assert.Equal(AirportQueueStatus.Removed, entry.Status);
        Assert.Equal(AirportQueueLeftReason.AdminRemoved, entry.LeftReason);
        Assert.False((await AirportFlow.QueueAsync(a.Client)).GetProperty("inQueue").GetBoolean());
        using var again = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue/{entryId}") { Content = JsonContent.Create(new { reason = "مرة أخرى" }) };
        Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(again)).StatusCode);
        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(l => l.Action == "airport_queue.remove").ToListAsync());
        Assert.Single(audit);
        Assert.Equal("سلوك غير لائق", JsonDocument.Parse(audit[0].AfterJson!).RootElement.GetProperty("reason").GetString());
        Assert.NotNull(b);
    }
}

/// <summary><c>Airport:RejectAction=remove</c> and <c>Airport:QueueMaxOffers=1</c>.</summary>
public class AirportQueueConfigurationTests(AirportRemoveFixture fixture) : IClassFixture<AirportRemoveFixture>
{
    [Fact]
    public async Task Reject_removes_the_driver_and_after_the_maximum_queue_offers_the_normal_search_starts()
    {
        var waiting = SchedulingFlow.Ruh.WaitingArea;
        var area = TripFlow.Area(0);
        var (a, aId) = await SchedulingFlow.DriverAsync(fixture, waiting, "أحمد");
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(2));
        var (b, bId) = await SchedulingFlow.DriverAsync(fixture, waiting, "بندر");
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var zoneId = await AirportFlow.ZoneIdAsync(passenger.Client, "T1-P1");
        var tripId = (await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", AirportFlow.PickupRequest(area, zoneId))).ReadJsonAsync()).GetProperty("id").GetString()!;

        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(a.Client);
        Assert.Equal(tripId, offer.GetProperty("tripId").GetString());
        (await a.Client.PostAsJsonAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/reject", new { })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        var removed = await AirportFlow.EntryAsync(fixture, aId);
        Assert.Equal(AirportQueueStatus.Removed, removed.Status);
        Assert.Equal(AirportQueueLeftReason.RejectedOffer, removed.LeftReason);
        Assert.False((await AirportFlow.QueueAsync(a.Client)).GetProperty("inQueue").GetBoolean());

        // One queue offer per trip: B (still queued) now gets the normal round, not a second queue offer.
        var next = await FavoritesFlow.ActiveOfferAsync(b.Client);
        Assert.Equal(tripId, next.GetProperty("tripId").GetString());
        using var admin = await fixture.LoginAdminAsync();
        var modes = (await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync()).GetProperty("attempts").EnumerateArray().Select(r => r.GetProperty("mode").GetString()).ToList();
        Assert.Equal(new[] { "airport_queue", "normal" }, modes.ToArray());
        Assert.Equal(AirportQueueStatus.Waiting, (await AirportFlow.EntryAsync(fixture, bId)).Status);
    }
}

public class AirportUnitTests
{
    [Theory]
    [InlineData("SV1020", true)]
    [InlineData("SV1020A", true)]
    [InlineData("A123", true)]
    [InlineData("EY1", true)]
    [InlineData("S1", false)]
    [InlineData("SV12345", false)]
    [InlineData("SV-1020", false)]
    [InlineData("SVV1020", false)]
    [InlineData("1020SV", false)]
    public void Flight_numbers_follow_the_documented_pattern_after_normalisation(string flight, bool valid) =>
        Assert.Equal(valid, FlightNumbers.IsValid(FlightNumbers.Normalize(flight)!));

    [Fact]
    public void Flight_numbers_are_upper_cased_without_spaces_and_blank_means_none()
    {
        Assert.Equal("SV1020A", FlightNumbers.Normalize(" sv 1020a "));
        Assert.Null(FlightNumbers.Normalize("   "));
        Assert.Null(FlightNumbers.Normalize(null));
    }

    [Fact]
    public void Queue_estimate_is_position_times_the_average_dispatch_interval_and_needs_two_dispatches()
    {
        var start = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);
        Assert.Null(AirportQueueService.EstimateOf(3, []));
        Assert.Null(AirportQueueService.EstimateOf(3, [start]));
        Assert.Equal(20, AirportQueueService.EstimateOf(2, [start, start.AddMinutes(10)]));
        Assert.Equal(15, AirportQueueService.EstimateOf(1, [start, start.AddMinutes(10), start.AddMinutes(30)]));
        Assert.Equal(8, AirportQueueService.EstimateOf(1, [start, start.AddMinutes(7.5)]));
    }

    [Fact]
    public void Waiting_policy_round_trips_through_the_documented_json_shape()
    {
        var json = new WaitingPolicy(15, 0.5m).ToJson();
        Assert.Equal("{\"freeMinutes\":15,\"perMinute\":0.5}", json);
        Assert.Equal(new WaitingPolicy(15, 0.5m), WaitingPolicy.Parse(json));
        Assert.Null(WaitingPolicy.Parse(null));
        Assert.Null(WaitingPolicy.Parse("not json"));
    }
}
