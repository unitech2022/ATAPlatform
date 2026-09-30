using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>The booking window is measured from the booking time: exactly 7 days is accepted, one minute more is not, and the minimum lead applies (doc 11 §F17.3).</summary>
public class ScheduleWindowTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Window_boundary_is_exactly_seven_days_from_the_booking_time_including_the_september_26_example()
    {
        var clock = fixture.Factory.Clock;
        clock.Set(new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc));
        var area = TripFlow.Area(0);
        var now = clock.UtcNow;

        var limit = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
        var exact = await SafetyFlow.PassengerAsync(fixture);
        var accepted = await SchedulingFlow.BookAsync(exact.Client, area, limit);
        Assert.Equal("scheduled", accepted.GetProperty("status").GetString());
        Assert.Equal(limit, accepted.GetProperty("scheduledAt").GetDateTime().ToUniversalTime());

        // One minute over → 422 with the latest allowed instant; the same check applies to the quote and its estimate alias.
        var over = await SafetyFlow.PassengerAsync(fixture);
        var tooFar = await over.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, limit.AddMinutes(1)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooFar.StatusCode);
        var error = (await tooFar.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("schedule_window_exceeded", error.GetProperty("code").GetString());
        Assert.Equal(limit, error.GetProperty("details").GetProperty("maxScheduledAt").GetDateTime().ToUniversalTime());
        foreach (var url in new[] { "/api/v1/pricing/quote", "/api/v1/passenger/trips/estimate" })
        {
            var quote = await over.Client.PostAsJsonAsync(url, SchedulingFlow.Request(area, limit.AddMinutes(1)));
            Assert.Equal("schedule_window_exceeded", await quote.ErrorCodeAsync());
            Assert.Equal(HttpStatusCode.OK, (await over.Client.PostAsJsonAsync(url, SchedulingFlow.Request(area, limit))).StatusCode);
        }

        // Booked three days later, the same instant is inside the window: the limit moves with the booking time, it is not a calendar-day cut-off.
        clock.Set(now.AddDays(3));
        Assert.Equal(HttpStatusCode.Created, (await over.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, limit.AddDays(2)))).StatusCode);
    }

    [Fact]
    public async Task Minimum_lead_time_and_the_rules_endpoint()
    {
        var clock = fixture.Factory.Clock;
        clock.Set(new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc));
        var area = TripFlow.Area(1);
        var now = clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);

        var tooSoon = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddMinutes(20)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooSoon.StatusCode);
        var error = (await tooSoon.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("schedule_lead_too_short", error.GetProperty("code").GetString());
        Assert.Equal(now.AddMinutes(30), error.GetProperty("details").GetProperty("minScheduledAt").GetDateTime().ToUniversalTime());
        Assert.Equal("schedule_lead_too_short", await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", SchedulingFlow.Request(area, now.AddMinutes(20)))).ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Created, (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddMinutes(30)))).StatusCode);

        var missing = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(3)) is var r ? new { pickup = new { name = "أ", address = "ب", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "أ", address = "ب", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "scheduled" } : r);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        Assert.Equal("required", (await missing.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("scheduledAt").GetString());

        var rules = await (await passenger.Client.GetAsync($"/api/v1/passenger/scheduling/rules?rideCategoryId={SeedIds.RideCategories.Economy}")).ReadJsonAsync();
        Assert.Equal(7, rules.GetProperty("maxDaysAhead").GetInt32());
        Assert.Equal(30, rules.GetProperty("minLeadMinutes").GetInt32());
        Assert.Equal(now.AddMinutes(30), rules.GetProperty("minScheduledAt").GetDateTime().ToUniversalTime());
        Assert.Equal(now.AddDays(7), rules.GetProperty("maxScheduledAt").GetDateTime().ToUniversalTime());
        Assert.Equal(60, rules.GetProperty("freeCancelMinutesBefore").GetInt32());
        Assert.Equal(10m, rules.GetProperty("lateCancelFee").GetDecimal());
        Assert.Equal(new[] { 1440, 60, 15 }, rules.GetProperty("reminderOffsets").EnumerateArray().Select(x => x.GetInt32()).ToArray());
    }
}

public class ScheduledBookingTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Scheduled_trip_is_not_the_active_trip_does_not_block_an_immediate_trip_and_is_limited_to_three_open()
    {
        var area = TripFlow.Area(2);
        var now = fixture.Factory.Clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var first = await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(2));
        var tripId = first.GetProperty("id").GetString()!;
        Assert.Equal("scheduled", first.GetProperty("status").GetString());
        Assert.Equal("scheduled", first.GetProperty("bookingType").GetString());
        Assert.Equal(JsonValueKind.Null, first.GetProperty("driver").ValueKind);
        var scheduling = first.GetProperty("scheduling");
        Assert.Equal(now.AddHours(2).AddMinutes(-60), scheduling.GetProperty("freeCancelUntil").GetDateTime().ToUniversalTime());
        Assert.Equal(now.AddHours(2).AddMinutes(-10), scheduling.GetProperty("searchStartsAt").GetDateTime().ToUniversalTime());
        Assert.Equal(JsonValueKind.Null, scheduling.GetProperty("reservation").ValueKind);
        Assert.Equal(JsonValueKind.Null, first.GetProperty("airport").ValueKind);

        // Not the active trip …
        Assert.Equal(JsonValueKind.Null, (await (await passenger.Client.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync()).ValueKind);
        var listed = await (await passenger.Client.GetAsync("/api/v1/passenger/trips/scheduled")).ReadJsonAsync();
        Assert.Equal(tripId, Assert.Single(listed.EnumerateArray()).GetProperty("id").GetString());
        var byStatus = await (await passenger.Client.GetAsync("/api/v1/passenger/trips?status=scheduled")).ReadJsonAsync();
        Assert.Single(byStatus.GetProperty("items").EnumerateArray());
        Assert.Empty((await (await passenger.Client.GetAsync("/api/v1/passenger/trips?status=active")).ReadJsonAsync()).GetProperty("items").EnumerateArray());

        // … so an immediate trip is still allowed (and the scheduled one keeps its status).
        var immediate = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area));
        Assert.Equal(HttpStatusCode.Created, immediate.StatusCode);
        Assert.Equal("searching", (await immediate.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal("scheduled", (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("status").GetString());
        Assert.Equal(TripStatus.Scheduled, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);

        // Up to three open scheduled trips; the fourth is refused, a cancelled one frees a slot.
        var second = await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(4));
        await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(5));
        var fourth = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(6)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, fourth.StatusCode);
        var error = (await fourth.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("scheduled_limit_reached", error.GetProperty("code").GetString());
        Assert.Equal(3, error.GetProperty("details").GetProperty("max").GetInt32());
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{second.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, now.AddHours(6)))).StatusCode);
        var ordered = (await (await passenger.Client.GetAsync("/api/v1/passenger/trips/scheduled")).ReadJsonAsync()).EnumerateArray().Select(t => t.GetProperty("scheduledAt").GetDateTime()).ToList();
        Assert.Equal(ordered.OrderBy(x => x).ToList(), ordered);
    }

    [Fact]
    public async Task Booking_creates_only_future_reminders_and_notifies_the_passenger()
    {
        var area = TripFlow.Area(3);
        var now = fixture.Factory.Clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        // Two hours ahead: the 24 h reminder is already in the past, the 60 and 15 minute ones are not.
        var soon = await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(2));
        var reminders = await SchedulingFlow.RemindersAsync(fixture, soon.GetProperty("id").GetString()!);
        Assert.Equal(new[] { 60, 15 }, reminders.Select(r => r.OffsetMinutes).ToArray());
        Assert.All(reminders, r => Assert.Equal(ReminderRecipientRole.Passenger, r.RecipientRole));
        Assert.Equal(now.AddHours(1), reminders[0].SendAt);

        var far = await SchedulingFlow.BookAsync(passenger.Client, area, now.AddHours(30));
        Assert.Equal(new[] { 1440, 60, 15 }, (await SchedulingFlow.RemindersAsync(fixture, far.GetProperty("id").GetString()!)).Select(r => r.OffsetMinutes).ToArray());
        var booked = await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledBooked);
        Assert.Equal(2, booked.Count);
        Assert.Contains("المنزل", booked[0].BodyAr);
    }

    [Fact]
    public async Task Reminders_are_sent_once_late_ones_are_skipped_and_cancelling_the_trip_cancels_them()
    {
        var area = TripFlow.Area(4);
        var clock = fixture.Factory.Clock;
        var start = clock.UtcNow;
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var at = start.AddHours(30);
        var trip = await SchedulingFlow.BookAsync(passenger.Client, area, at);
        var tripId = trip.GetProperty("id").GetString()!;

        // T − 24 h: the first reminder is due and is sent exactly once.
        clock.Set(at.AddMinutes(-1440));
        await fixture.Factory.RunScheduledRemindersAsync();
        await fixture.Factory.RunScheduledRemindersAsync();
        var sent = await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledReminder);
        Assert.Single(sent);
        Assert.Contains("1440", sent[0].BodyEn);
        Assert.Equal(new[] { ReminderStatus.Sent, ReminderStatus.Pending, ReminderStatus.Pending }, (await SchedulingFlow.RemindersAsync(fixture, tripId)).Select(r => r.Status).ToArray());

        // The job was down at T − 60: 11 minutes later that reminder is skipped rather than sent late; the 15 minute one is still on time.
        clock.Set(at.AddMinutes(-60 + 11));
        await fixture.Factory.RunScheduledRemindersAsync();
        var afterLate = await SchedulingFlow.RemindersAsync(fixture, tripId);
        Assert.Equal(new[] { ReminderStatus.Sent, ReminderStatus.Skipped, ReminderStatus.Pending }, afterLate.Select(r => r.Status).ToArray());
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledReminder));

        // Cancelling the trip cancels what is still pending.
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        Assert.Equal(new[] { ReminderStatus.Sent, ReminderStatus.Skipped, ReminderStatus.Cancelled }, (await SchedulingFlow.RemindersAsync(fixture, tripId)).Select(r => r.Status).ToArray());
        clock.Set(at.AddMinutes(-10));
        await fixture.Factory.RunScheduledRemindersAsync();
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledReminder));
    }

    [Fact]
    public async Task Scheduled_pricing_uses_the_time_multiplier_at_the_pickup_time_and_a_locked_normal_demand_level()
    {
        var area = TripFlow.Area(5);
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var now = clock.UtcNow;
        var zone = (await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync()).GetProperty("pickupZone").GetProperty("id").GetString();
        var overridden = await admin.PostAsJsonAsync("/api/v1/admin/demand-overrides", new { zoneId = zone, demandLevelCode = "high", reason = "حدث", endsAt = now.AddDays(10) });
        overridden.EnsureSuccessStatusCode();

        JsonElement Economy(JsonElement quote) => quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        var immediate = Economy(await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync());
        Assert.Equal(1.5m, immediate.GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());

        // 02:00 Riyadh (23:00 UTC) is inside the seeded night multiplier ×1.15; the current "high" demand is ignored for a scheduled booking.
        var night = new DateTime(now.Year, now.Month, now.Day, 23, 0, 0, DateTimeKind.Utc).AddDays(1);
        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", SchedulingFlow.Request(area, night))).ReadJsonAsync();
        var scheduled = Economy(quote);
        Assert.Equal(1m, scheduled.GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());
        Assert.Equal("normal", scheduled.GetProperty("demand").GetProperty("code").GetString());
        Assert.Equal(1.15m, scheduled.GetProperty("breakdown").GetProperty("timeMultiplier").GetDecimal());
        Assert.True(scheduled.GetProperty("total").GetDecimal() < immediate.GetProperty("total").GetDecimal() * 1.15m / 1.5m + 1m);

        // The trip request without a quote is priced the same way and the fare stored with the trip keeps the locked demand.
        var trip = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, night));
        var tripBody = await trip.ReadJsonAsync();
        Assert.Equal(scheduled.GetProperty("total").GetDecimal(), tripBody.GetProperty("estimatedFare").GetDecimal());
        var stored = await fixture.Factory.WithDbAsync(db => db.FareQuotes.AsNoTracking().SingleAsync(q => q.UsedTripId == Guid.Parse(tripBody.GetProperty("id").GetString()!)));
        Assert.Equal("normal", stored.DemandLevelCode);
        Assert.Equal(scheduled.GetProperty("total").GetDecimal(), stored.Total);

        // With the rule's lock switched off the current demand level applies again.
        var rules = await (await admin.GetAsync("/api/v1/admin/scheduled-ride-rules")).ReadJsonAsync();
        var global = rules.EnumerateArray().Single(r => r.GetProperty("cityId").ValueKind == JsonValueKind.Null && r.GetProperty("rideCategoryId").ValueKind == JsonValueKind.Null);
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(global.GetRawText())!;
        body.Remove("id"); body.Remove("createdAt"); body.Remove("updatedAt");
        body["lockDemandNormal"] = JsonSerializer.SerializeToElement(false);
        (await admin.PutAsJsonAsync($"/api/v1/admin/scheduled-ride-rules/{global.GetProperty("id").GetString()}", body)).EnsureSuccessStatusCode();
        var unlocked = Economy(await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", SchedulingFlow.Request(area, night))).ReadJsonAsync());
        Assert.Equal(1.5m, unlocked.GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());
        body["lockDemandNormal"] = JsonSerializer.SerializeToElement(true);
        (await admin.PutAsJsonAsync($"/api/v1/admin/scheduled-ride-rules/{global.GetProperty("id").GetString()}", body)).EnsureSuccessStatusCode();
    }
}
