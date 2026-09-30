using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Admin CRUD of airports and zones with audit, validation, cache invalidation and permissions (<c>airport.manage</c>).</summary>
public class AirportAdminTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private const decimal JedLat = 21.68m;
    private const decimal JedLng = 39.15m;

    private static object AirportBody(string code = "JED", Action<Dictionary<string, object?>>? configure = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["cityId"] = SeedIds.CityRiyadh, ["code"] = code, ["nameAr"] = "مطار الملك عبدالعزيز", ["nameEn"] = "King Abdulaziz International Airport", ["lat"] = JedLat, ["lng"] = JedLng,
            ["geofence"] = new[] { new[] { 21.60, 39.10 }, new[] { 21.60, 39.20 }, new[] { 21.75, 39.20 }, new[] { 21.75, 39.10 } },
            ["requiresPickupZone"] = true, ["defaultFreeWaitingMinutes"] = 20, ["defaultWaitingPerMinute"] = 0.5m, ["queueEnabled"] = true, ["isActive"] = true,
        };
        configure?.Invoke(body);
        return body;
    }

    private static object ZoneBody(string code, string kind = "pickup_zone", Action<Dictionary<string, object?>>? configure = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["kind"] = kind, ["code"] = code, ["terminalCode"] = kind == "driver_waiting_area" ? null : "T1", ["nameAr"] = "منطقة", ["nameEn"] = "Zone", ["polygon"] = null, ["lat"] = JedLat, ["lng"] = JedLng,
            ["instructionsAr"] = "تعليمات", ["instructionsEn"] = "Instructions", ["freeWaitingMinutes"] = null, ["waitingPerMinute"] = null, ["sortOrder"] = 1, ["isActive"] = true,
        };
        configure?.Invoke(body);
        return body;
    }

    [Fact]
    public async Task Airports_and_zones_crud_is_validated_audited_permission_protected_and_takes_effect_immediately()
    {
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var listed = await (await admin.GetAsync("/api/v1/admin/airports")).ReadJsonAsync();
        var ruh = Assert.Single(listed.EnumerateArray());
        Assert.Equal(16, ruh.GetProperty("zonesCount").GetInt32());
        Assert.True(ruh.GetProperty("queueEnabled").GetBoolean());
        Assert.Equal(15, ruh.GetProperty("defaultFreeWaitingMinutes").GetInt32());

        // Validation.
        foreach (var (configure, field) in new (Action<Dictionary<string, object?>>, string)[]
                 {
                     (b => b["code"] = "JEDD", "code"), (b => b["geofence"] = new[] { new[] { 1.0, 2.0 } }, "geofence"), (b => b["nameAr"] = "", "nameAr"), (b => b["lat"] = 120m, "lat"),
                     (b => b["cityId"] = Guid.NewGuid(), "cityId"), (b => b["defaultFreeWaitingMinutes"] = -1, "defaultFreeWaitingMinutes"),
                 })
        {
            var invalid = await admin.PostAsJsonAsync("/api/v1/admin/airports", AirportBody("JED", configure));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            Assert.True((await invalid.ReadJsonAsync()).GetProperty("error").GetProperty("details").TryGetProperty(field, out _), field);
        }

        // Create (the IATA code is stored upper-case) and read back without zones.
        var created = await admin.PostAsJsonAsync("/api/v1/admin/airports", AirportBody("jed"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var jed = await created.ReadJsonAsync();
        var jedId = jed.GetProperty("id").GetString()!;
        Assert.Equal("JED", jed.GetProperty("code").GetString());
        Assert.Equal(4, jed.GetProperty("geofence").GetArrayLength());
        Assert.Equal("airport_zones", "airport_zones");
        var fetched = await (await admin.GetAsync($"/api/v1/admin/airports/{jedId}")).ReadJsonAsync();
        Assert.Equal(0, fetched.GetProperty("zonesCount").GetInt32());
        Assert.False(fetched.TryGetProperty("zones", out _));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/airports", AirportBody("JED"))).StatusCode);

        // The new geofence is used at once (cache invalidated): resolve finds it and a pickup there needs a zone.
        var resolved = await (await passenger.Client.GetAsync($"/api/v1/passenger/airports/resolve?lat={JedLat}&lng={JedLng}")).ReadJsonAsync();
        Assert.Equal("JED", resolved.GetProperty("airport").GetProperty("code").GetString());
        Assert.Equal(0, resolved.GetProperty("pickupZones").GetArrayLength());

        // Zones: kinds, the mandatory terminal code / polygon, unique codes.
        var url = $"/api/v1/admin/airports/{jedId}/zones";
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync(url, ZoneBody("W1", "driver_waiting_area"))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync(url, ZoneBody("TERM", "terminal", b => b["terminalCode"] = null))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync(url, ZoneBody("bad code!"))).StatusCode);
        var polygon = new[] { new[] { 21.66, 39.13 }, new[] { 21.66, 39.14 }, new[] { 21.67, 39.14 }, new[] { 21.67, 39.13 } };
        var waiting = await admin.PostAsJsonAsync(url, ZoneBody("W1", "driver_waiting_area", b => b["polygon"] = polygon));
        Assert.Equal(HttpStatusCode.Created, waiting.StatusCode);
        Assert.Equal(4, (await waiting.ReadJsonAsync()).GetProperty("polygon").GetArrayLength());
        var pickup = await admin.PostAsJsonAsync(url, ZoneBody("P1", "pickup_zone", b => { b["freeWaitingMinutes"] = 30; b["waitingPerMinute"] = 1.5m; }));
        Assert.Equal(HttpStatusCode.Created, pickup.StatusCode);
        var pickupId = (await pickup.ReadJsonAsync()).GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync(url, ZoneBody("T1", "terminal"))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync(url, ZoneBody("P1", "pickup_zone"))).StatusCode);
        Assert.Equal(3, (await (await admin.GetAsync(url)).ReadJsonAsync()).GetArrayLength());
        var updated = await admin.PutAsJsonAsync($"{url}/{pickupId}", ZoneBody("P1", "pickup_zone", b => { b["nameEn"] = "Pickup 1"; b["freeWaitingMinutes"] = 30; b["waitingPerMinute"] = 1.5m; }));
        Assert.Equal("Pickup 1", (await updated.ReadJsonAsync()).GetProperty("nameEn").GetString());

        // A trip with a pickup zone fixes the airport's waiting policy (zone free minutes 30, airport per-minute default 0.5 is replaced by the zone's 1.5).
        var request = SchedulingFlow.Request(TripFlow.Area(0), DateTime.UtcNow, scheduled: false, airportPickupZoneId: Guid.Parse(pickupId), pickupLatOverride: JedLat, pickupLngOverride: JedLng);
        var trip = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", request)).ReadJsonAsync();
        Assert.Equal("JED", trip.GetProperty("airport").GetProperty("code").GetString());
        Assert.Equal(30, trip.GetProperty("airport").GetProperty("freeWaitingMinutes").GetInt32());
        var row = await SchedulingFlow.TripRowAsync(fixture, trip.GetProperty("id").GetString()!);
        Assert.Equal(1.5m, ATA.Domain.Trips.WaitingPolicy.Parse(row.WaitingPolicy)!.PerMinute);

        // Update and delete: an airport / zone that trips reference is deactivated instead of removed.
        (await admin.PutAsJsonAsync($"/api/v1/admin/airports/{jedId}", AirportBody("JED", b => b["nameEn"] = "Jeddah"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{url}/{pickupId}")).StatusCode);
        Assert.False((await fixture.Factory.WithDbAsync(db => db.AirportZones.AsNoTracking().SingleAsync(z => z.Id == Guid.Parse(pickupId)))).IsActive);
        var terminalId = (await (await admin.GetAsync(url)).ReadJsonAsync()).EnumerateArray().Single(z => z.GetProperty("code").GetString() == "T1").GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{url}/{terminalId}")).StatusCode);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.AirportZones.AnyAsync(z => z.Id == Guid.Parse(terminalId!))));
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/airports/{jedId}")).StatusCode);
        Assert.False((await fixture.Factory.WithDbAsync(db => db.Airports.AsNoTracking().SingleAsync(a => a.Id == Guid.Parse(jedId)))).IsActive);
        Assert.Equal(JsonValueKind.Null, (await (await passenger.Client.GetAsync($"/api/v1/passenger/airports/resolve?lat={JedLat}&lng={JedLng}")).ReadJsonAsync()).ValueKind);
        Assert.Single((await (await fixture.CreateClient().GetAsync("/api/v1/catalog/airports")).ReadJsonAsync()).EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/airports/{Guid.NewGuid()}/zones")).StatusCode);

        // An airport without trips is removed for good.
        var spare = (await (await admin.PostAsJsonAsync("/api/v1/admin/airports", AirportBody("DMM", b => b["geofence"] = new[] { new[] { 26.4, 49.7 }, new[] { 26.4, 49.8 }, new[] { 26.5, 49.8 } }))).ReadJsonAsync()).GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/airports/{spare}")).StatusCode);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.Airports.AnyAsync(a => a.Id == Guid.Parse(spare!))));

        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "airport" || a.EntityType == "airport_zone").Select(a => a.Action).ToListAsync());
        Assert.Equal(2, actions.Count(a => a == "airport.create"));
        Assert.Single(actions, a => a == "airport.update");
        Assert.Equal(2, actions.Count(a => a == "airport.delete"));
        Assert.Equal(3, actions.Count(a => a == "airport_zone.create"));
        Assert.Single(actions, a => a == "airport_zone.update");
        Assert.Equal(2, actions.Count(a => a == "airport_zone.delete"));

        var limited = await FavoritesFlow.LimitedAdminAsync(fixture, "airport_viewer", "[\"trips.view\"]");
        Assert.Equal(HttpStatusCode.Forbidden, (await limited.GetAsync("/api/v1/admin/airports")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await limited.GetAsync($"/api/v1/admin/airports/{SeedIds.AirportRuh}/queue")).StatusCode);
        var manager = await FavoritesFlow.LimitedAdminAsync(fixture, "airport_manager", "[\"airport.manage\"]");
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/v1/admin/airports")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/api/v1/admin/airports")).StatusCode);
    }

    [Fact]
    public async Task Every_scheduled_notification_has_arabic_and_english_templates_including_the_favorite_request()
    {
        var codes = new[]
        {
            NotificationTypes.ScheduledBooked, NotificationTypes.ScheduledReminder, NotificationTypes.ScheduledDriverReserved, NotificationTypes.ScheduledConfirmRequest,
            NotificationTypes.ScheduledReservationReleased, NotificationTypes.ScheduledFavoriteRequest, NotificationTypes.ScheduledRematched,
        };
        var templates = await fixture.Factory.WithDbAsync(db => db.NotificationTemplates.AsNoTracking().Where(t => codes.Contains(t.Code)).ToListAsync());
        foreach (var code in codes)
        {
            var rows = templates.Where(t => t.Code == code).ToList();
            Assert.NotEmpty(rows);
            Assert.All(rows, t =>
            {
                Assert.False(string.IsNullOrWhiteSpace(t.BodyAr));
                Assert.False(string.IsNullOrWhiteSpace(t.BodyEn));
                Assert.True(t.IsActive);
            });
        }

        Assert.Contains("{passengerName}", templates.First(t => t.Code == NotificationTypes.ScheduledFavoriteRequest).BodyAr);
    }
}
