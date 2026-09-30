using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Admin console of scheduled rides: rules CRUD with audit and validation, the trips list, manual assignment / release and the KPIs.</summary>
public class ScheduledAdminTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static object RuleBody(Guid? cityId = null, Guid? categoryId = null, Action<Dictionary<string, object?>>? configure = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["cityId"] = cityId, ["rideCategoryId"] = categoryId, ["maxDaysAhead"] = 7, ["minLeadMinutes"] = 30, ["maxOpenPerPassenger"] = 3, ["lockDemandNormal"] = true, ["marketplaceEnabled"] = true,
            ["marketplaceRadiusKm"] = 30, ["favoriteExclusiveMinutes"] = 30, ["driverAssignmentLeadMinutes"] = 60, ["confirmationTimeoutMinutes"] = 10, ["finalConfirmationMinutesBefore"] = 15,
            ["finalConfirmationTimeoutMinutes"] = 5, ["searchStartMinutesBefore"] = 10, ["riderReminderOffsets"] = new[] { 1440, 60, 15 }, ["driverReminderOffsets"] = new[] { 1440, 180 },
            ["freeCancelMinutesBefore"] = 60, ["lateCancelFeeType"] = "fixed", ["lateCancelFeeAmount"] = 10m, ["lateCancelFeePercent"] = null, ["lateCancelDriverCompensationPercent"] = 50m,
            ["driverFreeReleaseMinutesBefore"] = 120, ["driverLateReleasePenaltyPoints"] = 3, ["driverConfirmationMissedPenaltyPoints"] = 3, ["driverNoShowPenaltyPoints"] = 6,
            ["driverNoShowGraceMinutes"] = 10, ["maxReservationsPerDriver"] = 5, ["reservationGapMinutes"] = 30, ["isActive"] = true,
        };
        configure?.Invoke(body);
        return body;
    }

    [Fact]
    public async Task Rules_are_seeded_selected_by_specificity_validated_audited_and_permission_protected()
    {
        using var admin = await fixture.LoginAdminAsync();
        var list = await (await admin.GetAsync("/api/v1/admin/scheduled-ride-rules")).ReadJsonAsync();
        var seeded = list.EnumerateArray().Single();
        Assert.Equal(JsonValueKind.Null, seeded.GetProperty("cityId").ValueKind);
        Assert.Equal(7, seeded.GetProperty("maxDaysAhead").GetInt32());
        Assert.Equal(new[] { 1440, 60, 15 }, seeded.GetProperty("riderReminderOffsets").EnumerateArray().Select(x => x.GetInt32()).ToArray());
        Assert.Equal("fixed", seeded.GetProperty("lateCancelFeeType").GetString());

        // A category rule limiting comfort to one open scheduled trip overrides the global rule for that category only.
        var created = await admin.PostAsJsonAsync("/api/v1/admin/scheduled-ride-rules", RuleBody(null, SeedIds.RideCategories.Comfort, b => b["maxOpenPerPassenger"] = 1));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rule = await created.ReadJsonAsync();
        var ruleId = rule.GetProperty("id").GetString();
        var area = TripFlow.Area(0);
        var now = fixture.Factory.Clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(3), rideCategoryId: SeedIds.RideCategories.Comfort);
        var second = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(4), rideCategoryId: SeedIds.RideCategories.Comfort));
        Assert.Equal("scheduled_limit_reached", await second.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Created, (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(4)))).StatusCode);
        var scoped = await (await passenger.Client.GetAsync($"/api/v1/passenger/scheduling/rules?rideCategoryId={SeedIds.RideCategories.Comfort}")).ReadJsonAsync();
        Assert.Equal(7, scoped.GetProperty("maxDaysAhead").GetInt32());

        // A rule with a wider window for the city + category wins over the category rule; an inactive rule is ignored.
        var cityRule = await admin.PostAsJsonAsync("/api/v1/admin/scheduled-ride-rules", RuleBody(SeedIds.CityRiyadh, SeedIds.RideCategories.Comfort, b => { b["maxDaysAhead"] = 14; b["isActive"] = false; }));
        Assert.Equal(HttpStatusCode.Created, cityRule.StatusCode);
        Assert.Equal(7, (await (await passenger.Client.GetAsync($"/api/v1/passenger/scheduling/rules?rideCategoryId={SeedIds.RideCategories.Comfort}&lat={area.Lat}&lng={area.Lng}")).ReadJsonAsync()).GetProperty("maxDaysAhead").GetInt32());
        var cityRuleId = (await cityRule.ReadJsonAsync()).GetProperty("id").GetString();
        (await admin.PutAsJsonAsync($"/api/v1/admin/scheduled-ride-rules/{cityRuleId}", RuleBody(SeedIds.CityRiyadh, SeedIds.RideCategories.Comfort, b => b["maxDaysAhead"] = 14))).EnsureSuccessStatusCode();
        Assert.Equal(14, (await (await passenger.Client.GetAsync($"/api/v1/passenger/scheduling/rules?rideCategoryId={SeedIds.RideCategories.Comfort}&lat={area.Lat}&lng={area.Lng}")).ReadJsonAsync()).GetProperty("maxDaysAhead").GetInt32());

        // One rule per (city, category); validation names the offending fields.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/admin/scheduled-ride-rules", RuleBody(null, SeedIds.RideCategories.Comfort))).StatusCode);
        var invalid = await admin.PostAsJsonAsync("/api/v1/admin/scheduled-ride-rules", RuleBody(null, SeedIds.RideCategories.Premium, b =>
        {
            b["maxDaysAhead"] = 0; b["finalConfirmationMinutesBefore"] = 70; b["searchStartMinutesBefore"] = 80; b["lateCancelFeeType"] = "percent"; b["lateCancelFeePercent"] = null;
            b["riderReminderOffsets"] = new[] { -5 }; b["lateCancelDriverCompensationPercent"] = 120m;
        }));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        var details = (await invalid.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        foreach (var field in new[] { "maxDaysAhead", "finalConfirmationMinutesBefore", "searchStartMinutesBefore", "lateCancelFeePercent", "riderReminderOffsets", "lateCancelDriverCompensationPercent" })
        {
            Assert.True(details.TryGetProperty(field, out _), field);
        }

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/scheduled-ride-rules/{ruleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/scheduled-ride-rules/{cityRuleId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/v1/admin/scheduled-ride-rules/{ruleId}")).StatusCode);
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "scheduled_ride_rule").Select(a => a.Action).ToListAsync());
        Assert.Equal(2, actions.Count(a => a == "scheduled_ride_rule.create"));
        Assert.Single(actions, a => a == "scheduled_ride_rule.update");
        Assert.Equal(2, actions.Count(a => a == "scheduled_ride_rule.delete"));

        // Permissions: only scheduling.manage opens the console.
        var noAccess = await FavoritesFlow.LimitedAdminAsync(fixture, "schedule_viewer", "[\"trips.view\"]");
        Assert.Equal(HttpStatusCode.Forbidden, (await noAccess.GetAsync("/api/v1/admin/scheduled-ride-rules")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await noAccess.GetAsync("/api/v1/admin/scheduled-trips")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await noAccess.GetAsync("/api/v1/admin/scheduling/stats")).StatusCode);
        var manager = await FavoritesFlow.LimitedAdminAsync(fixture, "schedule_manager", "[\"scheduling.manage\"]");
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/api/v1/admin/scheduled-ride-rules")).StatusCode);
    }

    [Fact]
    public async Task Trips_list_filters_at_risk_manual_assignment_and_release_are_audited_and_idempotent()
    {
        var area = TripFlow.Area(1);
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var now = clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SchedulingFlow.DriverAsync(fixture, area);
        var (other, otherId) = await SchedulingFlow.DriverAsync(fixture, area, "بندر");
        var soon = (await SchedulingFlow.BookAsync(passenger.Client, area, now.AddMinutes(80))).GetProperty("id").GetString()!;
        var later = (await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(6))).GetProperty("id").GetString()!;
        var comfort = (await SchedulingFlow.BookAsync((await SafetyFlow.PassengerAsync(fixture)).Client, area, now.AddHours(7), rideCategoryId: SeedIds.RideCategories.Comfort)).GetProperty("id").GetString()!;

        async Task<List<JsonElement>> List(string query = "") =>
            (await (await admin.GetAsync($"/api/v1/admin/scheduled-trips?pageSize=200{(query.Length > 0 ? "&" : "")}{query}")).ReadJsonAsync()).GetProperty("items").EnumerateArray().ToList();
        JsonElement Row(List<JsonElement> rows, string id) => rows.Single(r => r.GetProperty("tripId").GetString() == id);
        var rows = await List();
        var soonRow = Row(rows, soon);
        Assert.Equal("none", soonRow.GetProperty("reservationStatus").GetString());
        Assert.True(soonRow.GetProperty("atRisk").GetBoolean());
        Assert.Equal(80, soonRow.GetProperty("minutesToPickup").GetInt32());
        Assert.Equal("scheduled", soonRow.GetProperty("status").GetString());
        Assert.Equal(SeedIds.RideCategories.Economy.ToString(), soonRow.GetProperty("rideCategoryId").GetString());
        Assert.NotEqual(JsonValueKind.Null, soonRow.GetProperty("zoneId").ValueKind);
        Assert.False(Row(rows, later).GetProperty("atRisk").GetBoolean());
        Assert.Equal(new[] { soon, later, comfort }, rows.Select(r => r.GetProperty("tripId").GetString()!).Where(id => id == soon || id == later || id == comfort).ToArray());
        Assert.Contains(await List("atRisk=true"), r => r.GetProperty("tripId").GetString() == soon);
        Assert.DoesNotContain(await List("atRisk=true"), r => r.GetProperty("tripId").GetString() == later);
        Assert.DoesNotContain(await List($"rideCategoryId={SeedIds.RideCategories.Economy}"), r => r.GetProperty("tripId").GetString() == comfort);
        Assert.Contains(await List($"cityId={SeedIds.CityRiyadh}"), r => r.GetProperty("tripId").GetString() == soon);
        Assert.DoesNotContain(await List($"cityId={Guid.NewGuid()}"), r => r.GetProperty("tripId").GetString() == soon);
        var day = DateOnly.FromDateTime(now.AddHours(3)).ToString("yyyy-MM-dd");
        Assert.Contains(await List($"from={day}&to={day}"), r => r.GetProperty("tripId").GetString() == soon);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync("/api/v1/admin/scheduled-trips?reservation=bogus")).StatusCode);

        // Manual assignment (source = admin, overlap rules apply), audited and idempotent for the same driver.
        var assigned = await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/assign", new { driverId });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);
        var assignedRow = await assigned.ReadJsonAsync();
        Assert.Equal("reserved", assignedRow.GetProperty("reservationStatus").GetString());
        Assert.Equal(driverId.ToString(), assignedRow.GetProperty("driverId").GetString());
        Assert.Equal(ReservationSource.Admin, (await SchedulingFlow.ReservationAsync(fixture, later)).Source);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/assign", new { driverId })).StatusCode);
        Assert.Single(await SchedulingFlow.ReservationsAsync(fixture, later));
        Assert.Equal("reservation_taken", await (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/assign", new { driverId = otherId })).ErrorCodeAsync());
        Assert.Contains(await List("reservation=reserved"), r => r.GetProperty("tripId").GetString() == later);
        Assert.DoesNotContain(await List("reservation=none"), r => r.GetProperty("tripId").GetString() == later);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/assign", new { })).StatusCode);
        var unapproved = await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{soon}/assign", new { driverId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unapproved.StatusCode);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, ATA.Domain.Notifications.NotificationTypes.ScheduledDriverReserved));

        // Release: a reason is required, the reservation ends without points, a repeated release is a no-op, and the trip can be assigned to another driver.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/release-reservation", new { })).StatusCode);
        var released = await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/release-reservation", new { reason = "طلب الكابتن" });
        Assert.Equal(HttpStatusCode.OK, released.StatusCode);
        Assert.Equal("none", (await released.ReadJsonAsync()).GetProperty("reservationStatus").GetString());
        var reservation = await SchedulingFlow.ReservationAsync(fixture, later);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(ReservationReleaseReason.Admin, reservation.ReleaseReason);
        Assert.Equal(0, reservation.PenaltyPoints);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/release-reservation", new { reason = "مرة أخرى" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{later}/assign", new { driverId = otherId })).StatusCode);
        Assert.Equal(otherId, (await SchedulingFlow.TripRowAsync(fixture, later)).ReservedDriverId);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/api/v1/admin/scheduled-trips/{Guid.NewGuid()}/release-reservation", new { reason = "x" })).StatusCode);

        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "scheduled_trip").Select(a => a.Action).ToListAsync());
        Assert.Equal(2, actions.Count(a => a == "scheduled_trip.assign"));
        Assert.Single(actions, a => a == "scheduled_trip.release");
        Assert.NotNull(other);

        // The admin trip detail lists every reservation and reminder of the trip.
        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{later}")).ReadJsonAsync();
        var scheduling = detail.GetProperty("scheduling");
        Assert.Equal(2, scheduling.GetProperty("reservations").GetArrayLength());
        Assert.Equal("released", scheduling.GetProperty("reservations")[0].GetProperty("status").GetString());
        Assert.Equal("admin", scheduling.GetProperty("reservations")[0].GetProperty("releaseReason").GetString());
        Assert.Equal("reserved", scheduling.GetProperty("reservation").GetProperty("status").GetString());
        Assert.Equal("بندر", scheduling.GetProperty("reservation").GetProperty("driverName").GetString());
        Assert.True(scheduling.GetProperty("reminders").GetArrayLength() >= 3);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("airport").ValueKind);
    }

    [Fact]
    public async Task Stats_count_bookings_cancellations_releases_no_shows_and_the_rates()
    {
        var area = TripFlow.Area(2);
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var baseline = await (await admin.GetAsync("/api/v1/admin/scheduling/stats")).ReadJsonAsync();
        var start = clock.UtcNow;
        var at = start.AddHours(3);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);

        // 1: a full trip (completed); 2: cancelled free by the rider; 3: cancelled late by the rider with a reservation; 4: driver misses the first confirmation.
        var p1 = await SafetyFlow.PassengerAsync(fixture);
        var completed = (await SchedulingFlow.BookAsync(p1.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, completed);
        var p2 = await SafetyFlow.PassengerAsync(fixture);
        var freeCancelled = (await SchedulingFlow.BookAsync(p2.Client, area, at.AddHours(3))).GetProperty("id").GetString()!;
        (await p2.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{freeCancelled}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var (lateDriver, _) = await SchedulingFlow.DriverAsync(fixture, area, "خالد");
        var p3 = await SafetyFlow.PassengerAsync(fixture);
        var lateCancelled = (await SchedulingFlow.BookAsync(p3.Client, area, at.AddHours(6))).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(lateDriver.Client, lateCancelled);
        var (missDriver, _) = await SchedulingFlow.DriverAsync(fixture, area, "فهد");
        var p4 = await SafetyFlow.PassengerAsync(fixture);
        var missed = (await SchedulingFlow.BookAsync(p4.Client, area, at.AddHours(9))).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(missDriver.Client, missed);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(driver.Client, completed);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        await SchedulingFlow.ConfirmAsync(driver.Client, completed);
        await TripFlow.DriveAsync(driver.Client, completed, (await SchedulingFlow.PassengerTripAsync(p1.Client, completed)).GetProperty("pin").GetString()!);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{completed}/complete", new { })).EnsureSuccessStatusCode();
        clock.Set(at.AddHours(6).AddMinutes(-30));
        (await p3.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateCancelled}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        await SchedulingFlow.AtAsync(fixture, at.AddHours(9).AddMinutes(-60));
        await SchedulingFlow.AtAsync(fixture, at.AddHours(9).AddMinutes(-60 + 11));

        var stats = await (await admin.GetAsync("/api/v1/admin/scheduling/stats")).ReadJsonAsync();
        int Delta(string name) => stats.GetProperty(name).GetInt32() - baseline.GetProperty(name).GetInt32();
        Assert.Equal(4, Delta("booked"));
        Assert.Equal(1, Delta("completed"));
        Assert.Equal(2, Delta("cancelledByPassenger"));
        Assert.Equal(1, Delta("cancelledLate"));
        Assert.Equal(1, Delta("confirmationMissed"));
        Assert.Equal(0, Delta("driverNoShows"));
        Assert.Equal(0, Delta("driverReleases"));
        Assert.True(stats.GetProperty("scheduledCompletionRate").GetDecimal() is > 0m and <= 1m);
        Assert.True(stats.GetProperty("scheduledCancellationRate").GetDecimal() is > 0m and <= 1m);
        Assert.True(stats.GetProperty("avgReservationLeadHours").GetDecimal() > 0m);
        Assert.Equal(0m, stats.GetProperty("driverNoShowRate").GetDecimal());

        // Restricted to a fresh window the numbers are exact: 4 bookings, 1 completed, 2 cancelled (50 %), 1 completed of 3 due (the free cancellation is left out).
        var today = DateOnly.FromDateTime(start).ToString("yyyy-MM-dd");
        var windowed = await (await admin.GetAsync($"/api/v1/admin/scheduling/stats?from={today}&to={today}")).ReadJsonAsync();
        Assert.True(windowed.GetProperty("booked").GetInt32() >= 4);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync("/api/v1/admin/scheduling/stats?from=2026-10-05&to=2026-10-01")).StatusCode);
    }
}
