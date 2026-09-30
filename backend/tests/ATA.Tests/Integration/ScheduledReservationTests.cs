using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>The scheduled timeline with a fake clock: reserve → T−60 first confirmation → T−15 final confirmation → assigned → completed (doc 11 §F17.3).</summary>
public class ScheduledTimelineTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    private static async Task<JsonElement> ReliabilityAsync(SafetyFlow.Party driver) => await (await driver.Client.GetAsync("/api/v1/driver/reliability")).ReadJsonAsync();

    private static async Task<JsonElement> ActiveReservationsAsync(HttpClient driver) => (await (await driver.GetAsync("/api/v1/driver/scheduled?status=active")).ReadJsonAsync()).GetProperty("items");

    [Fact]
    public async Task Reserve_confirm_at_T_minus_60_and_T_minus_15_assigns_the_driver_and_the_trip_completes_without_surge()
    {
        var area = TripFlow.Area(0);
        var clock = fixture.Factory.Clock;
        var at = clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SchedulingFlow.DriverAsync(fixture, area);
        var trip = await SchedulingFlow.BookAsync(passenger.Client, area, at);
        var tripId = trip.GetProperty("id").GetString()!;

        // Marketplace: approximate pickup, no passenger identity.
        var market = await SchedulingFlow.MarketplaceAsync(driver.Client);
        var item = market.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("tripId").GetString() == tripId);
        Assert.Equal("economy", item.GetProperty("rideCategory").GetProperty("code").GetString());
        Assert.Equal(area.Lat, item.GetProperty("pickupApprox").GetProperty("lat").GetDecimal());
        Assert.Equal(0m, item.GetProperty("distanceToPickupKm").GetDecimal());
        Assert.False(item.TryGetProperty("passengerFirstName", out _));
        Assert.False(item.TryGetProperty("pickup", out _));
        Assert.True(item.GetProperty("driverNetEarnings").GetDecimal() is > 0m && item.GetProperty("driverNetEarnings").GetDecimal() < item.GetProperty("estimatedFare").GetDecimal());
        Assert.False(item.GetProperty("isAirport").GetBoolean());
        Assert.False(item.GetProperty("isFavoriteRequest").GetBoolean());

        // Reserve: exact pickup and the passenger's first name only from now on.
        var reserved = await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        Assert.Equal("reserved", reserved.GetProperty("status").GetString());
        Assert.Equal("marketplace", reserved.GetProperty("source").GetString());
        Assert.Equal("سارة", reserved.GetProperty("passengerFirstName").GetString());
        Assert.Equal("المنزل", reserved.GetProperty("pickup").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, reserved.GetProperty("confirmDeadline").ValueKind);
        Assert.Equal(at.AddMinutes(-120), reserved.GetProperty("freeReleaseUntil").GetDateTime().ToUniversalTime());
        Assert.False(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(driver.Client), tripId));
        var passengerView = await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId);
        var visible = passengerView.GetProperty("scheduling").GetProperty("reservation");
        Assert.Equal("reserved", visible.GetProperty("status").GetString());
        Assert.Equal("محمد", visible.GetProperty("driverFirstName").GetString());
        Assert.Equal("Camry", visible.GetProperty("vehicle").GetProperty("model").GetString());
        Assert.Equal(driverId, (await SchedulingFlow.TripRowAsync(fixture, tripId)).ReservedDriverId);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledDriverReserved));
        var plan = await SchedulingFlow.RemindersAsync(fixture, tripId);
        Assert.Contains(plan, r => r.Kind == ReminderKind.ConfirmRequest && r.SendAt == at.AddMinutes(-60) && r.RecipientRole == ReminderRecipientRole.Driver);
        Assert.Contains(plan, r => r.Kind == ReminderKind.FinalConfirmRequest && r.SendAt == at.AddMinutes(-15));

        // Nothing is due before T − 60; the request goes out at T − 60 with a 10 minute deadline.
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-61));
        Assert.Equal(JsonValueKind.Null, (await ActiveReservationsAsync(driver.Client)).EnumerateArray().First().GetProperty("confirmDeadline").ValueKind);
        var notConfirmable = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, notConfirmable.StatusCode);
        Assert.Equal("not_due", (await notConfirmable.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        var requested = (await ActiveReservationsAsync(driver.Client)).EnumerateArray().Single();
        Assert.Equal(at.AddMinutes(-50), requested.GetProperty("confirmDeadline").GetDateTime().ToUniversalTime());
        var request = Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, driver.UserId, NotificationTypes.ScheduledConfirmRequest));
        Assert.Contains("10", request.BodyEn);
        Assert.Equal("confirmed", (await SchedulingFlow.ConfirmAsync(driver.Client, tripId)).GetProperty("status").GetString());

        // T − 15: the final confirmation; confirming assigns the driver and returns the trip.
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        var finalRequest = (await ActiveReservationsAsync(driver.Client)).EnumerateArray().Single();
        Assert.Equal(at.AddMinutes(-10), finalRequest.GetProperty("finalConfirmDeadline").GetDateTime().ToUniversalTime());
        Assert.Equal(2, (await SchedulingFlow.NotificationsAsync(fixture, driver.UserId, NotificationTypes.ScheduledConfirmRequest)).Count);
        var assigned = await SchedulingFlow.ConfirmAsync(driver.Client, tripId);
        Assert.Equal("assigned", assigned.GetProperty("status").GetString());
        Assert.Equal("driver_assigned", assigned.GetProperty("trip").GetProperty("status").GetString());
        Assert.Equal(driverId.ToString(), assigned.GetProperty("trip").GetProperty("driver").GetProperty("id").GetString());
        Assert.Equal(tripId, (await (await driver.Client.GetAsync("/api/v1/driver/trips/active")).ReadJsonAsync()).GetProperty("id").GetString());
        var pin = (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("pin").GetString()!;
        Assert.Matches("^[0-9]{4}$", pin);
        Assert.Equal(TripStatus.DriverAssigned, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.TripDriverAssigned));

        // From here it is an ordinary F8 trip; the completed fare has no surge and the reservation completes.
        await TripFlow.DriveAsync(driver.Client, tripId, pin);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Completed, row.Status);
        Assert.Equal(TripFlow.RoundToHalf(TripFlow.EconomyCore(row.FinalDistanceM!.Value, row.FinalDurationS!.Value, row.WaitingSeconds) + 2m), row.FinalFare);
        Assert.Equal(ReservationStatus.Completed, (await SchedulingFlow.ReservationAsync(fixture, tripId)).Status);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AnyAsync(e => e.TripId == row.Id)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        Assert.Equal("completed", (await (await driver.Client.GetAsync("/api/v1/driver/scheduled?status=history")).ReadJsonAsync()).GetProperty("items").EnumerateArray().Single().GetProperty("status").GetString());
    }

    [Fact]
    public async Task Missed_first_confirmation_releases_with_penalty_points_and_returns_the_trip_to_the_market()
    {
        var area = TripFlow.Area(1);
        var at = fixture.Factory.Clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, tripId);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60 + 9));
        Assert.Equal(ReservationStatus.Reserved, (await SchedulingFlow.ReservationAsync(fixture, tripId)).Status);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60 + 11));

        var released = await SchedulingFlow.ReservationAsync(fixture, tripId);
        Assert.Equal(ReservationStatus.Released, released.Status);
        Assert.Equal(ReservationReleaseReason.ConfirmationMissed, released.ReleaseReason);
        Assert.Equal(3, released.PenaltyPoints);
        Assert.Null((await SchedulingFlow.TripRowAsync(fixture, tripId)).ReservedDriverId);
        Assert.Equal(TripStatus.Scheduled, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);
        Assert.True(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(driver.Client), tripId));
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, driver.UserId, NotificationTypes.ScheduledReservationReleased));
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledReservationReleased));
        var reliability = await ReliabilityAsync(driver);
        Assert.Equal(3, reliability.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(1, reliability.GetProperty("cancellationsAtFault").GetInt32());
        Assert.Equal(1, reliability.GetProperty("tripsAccepted").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await driver.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/confirm", null)).StatusCode);
        Assert.Equal(driverId, (await SchedulingFlow.ReservationAsync(fixture, tripId)).DriverId);
        var history = (await (await driver.Client.GetAsync("/api/v1/driver/scheduled?status=history")).ReadJsonAsync()).GetProperty("items").EnumerateArray().Single();
        Assert.Equal("released", history.GetProperty("status").GetString());
        Assert.Equal(3, history.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(JsonValueKind.Null, history.GetProperty("passengerFirstName").ValueKind);
    }

    [Fact]
    public async Task Missed_final_confirmation_starts_the_search_at_once_and_tells_the_passenger()
    {
        var area = TripFlow.Area(2);
        var at = fixture.Factory.Clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15 + 4));
        Assert.Equal(TripStatus.Scheduled, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15 + 6));
        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Searching, row.Status);
        Assert.Null(row.ReservedDriverId);
        var reservation = await SchedulingFlow.ReservationAsync(fixture, tripId);
        Assert.Equal(ReservationReleaseReason.FinalConfirmationMissed, reservation.ReleaseReason);
        Assert.Equal(3, reservation.PenaltyPoints);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledRematched));
        Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.Rematched));
        Assert.Equal("searching", (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("status").GetString());
        Assert.Equal(3, (await ReliabilityAsync(driver)).GetProperty("penaltyPoints").GetInt32());
    }

    [Fact]
    public async Task Without_a_reservation_the_normal_search_starts_ten_minutes_before_pickup()
    {
        var area = TripFlow.Area(3);
        var at = fixture.Factory.Clock.UtcNow.AddMinutes(90);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-11));
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(TripStatus.Scheduled, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(driver.Client)).ValueKind);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-10));
        Assert.Equal(TripStatus.Searching, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);
        Assert.Contains(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.SearchStarted), e => e.Data!.Contains("scheduled_window"));
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.Equal(tripId, offer.GetProperty("tripId").GetString());
        // The search timeout counts from the moment the search started (not from the booking).
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(10));
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        Assert.Equal("driver_assigned", (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Final_confirmation_needs_an_online_driver_without_a_running_trip()
    {
        var area = TripFlow.Area(4);
        var at = fixture.Factory.Clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));

        (await driver.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        var offline = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/confirm", null);
        Assert.Equal(HttpStatusCode.Conflict, offline.StatusCode);
        var error = (await offline.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("reservation_not_confirmable", error.GetProperty("code").GetString());
        Assert.Equal("offline", error.GetProperty("details").GetProperty("reason").GetString());

        // Back online but on another trip.
        await TripFlow.GoOnlineAsync(driver.Client, area.Lat, area.Lng);
        var other = await SafetyFlow.PassengerAsync(fixture);
        var busy = await TripFlow.RequestAndAssignAsync(fixture, other.Client, driver.Client, TripFlow.Request(area));
        var onTrip = await driver.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/confirm", null);
        Assert.Equal("on_trip", (await onTrip.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());

        // Free again within the deadline → the confirmation goes through.
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{busy.GetProperty("id").GetString()}/cancel", new { reasonCode = "other", note = "اختبار" })).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal("assigned", (await SchedulingFlow.ConfirmAsync(driver.Client, tripId)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Driver_no_show_at_T_plus_10_rematches_records_points_and_creates_no_cancellation_event()
    {
        var area = TripFlow.Area(5);
        var at = fixture.Factory.Clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(9));
        Assert.Equal(TripStatus.DriverAssigned, (await SchedulingFlow.TripRowAsync(fixture, tripId)).Status);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(10));

        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Searching, row.Status);
        Assert.Null(row.DriverId);
        Assert.Null(row.VehicleId);
        Assert.Null(row.AssignedAt);
        var reservation = await SchedulingFlow.ReservationAsync(fixture, tripId);
        Assert.Equal(ReservationStatus.NoShow, reservation.Status);
        Assert.Equal(6, reservation.PenaltyPoints);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AnyAsync(e => e.TripId == row.Id)));
        Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.DriverNoShow));
        Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.DriverUnassigned));
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledRematched));
        Assert.Null((await fixture.Factory.WithDbAsync(db => db.Drivers.AsNoTracking().FirstAsync(d => d.Id == driverId))).CurrentTripId);
        var reliability = await ReliabilityAsync(driver);
        Assert.Equal(6, reliability.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(1, reliability.GetProperty("noShowCount").GetInt32());
        Assert.Equal("searching", (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("status").GetString());
        var preview = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("before_accept", preview.GetProperty("stage").GetString());
        Assert.Equal(0m, preview.GetProperty("fee").GetDecimal());
    }

    [Fact]
    public async Task Driver_cancelling_an_assigned_scheduled_trip_rematches_it_and_keeps_the_penalty_from_the_f14_rule()
    {
        var area = TripFlow.Area(6);
        var at = fixture.Factory.Clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        await SchedulingFlow.ConfirmAsync(driver.Client, tripId);

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var preview = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/cancel/preview", new { reasonCode = "pickup_too_far" })).ReadJsonAsync();
        Assert.Equal(2, preview.GetProperty("penaltyPoints").GetInt32());
        var cancelled = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/cancel", new { reasonCode = "pickup_too_far", expectedPenaltyPoints = 2 });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var body = await cancelled.ReadJsonAsync();
        Assert.Equal("searching", body.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("driver").ValueKind);

        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Searching, row.Status);
        Assert.Null(row.DriverId);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AnyAsync(e => e.TripId == row.Id)));
        var reservation = await SchedulingFlow.ReservationAsync(fixture, tripId);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(ReservationReleaseReason.DriverReleased, reservation.ReleaseReason);
        Assert.Equal(2, reservation.PenaltyPoints);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledRematched));
        Assert.Equal(2, (await ReliabilityAsync(driver)).GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(JsonValueKind.Null, (await (await driver.Client.GetAsync("/api/v1/driver/trips/active")).ReadJsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Releasing_a_reservation_is_free_before_T_minus_120_and_costs_points_after()
    {
        var area = TripFlow.Area(7);
        var clock = fixture.Factory.Clock;
        var at = clock.UtcNow.AddHours(5);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;

        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        var free = await driver.Client.PostAsJsonAsync($"/api/v1/driver/scheduled/{tripId}/release", new { reason = "ظرف طارئ" });
        Assert.Equal(HttpStatusCode.OK, free.StatusCode);
        var freeBody = await free.ReadJsonAsync();
        Assert.Equal("released", freeBody.GetProperty("status").GetString());
        Assert.Equal(0, freeBody.GetProperty("penaltyPoints").GetInt32());
        Assert.False((await SchedulingFlow.ReservationAsync(fixture, tripId)).IsLateRelease);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.ScheduledReservationReleased));
        Assert.True(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(driver.Client), tripId));
        Assert.Equal(0, (await ReliabilityAsync(driver)).GetProperty("penaltyPoints").GetInt32());

        await SchedulingFlow.ReserveAsync(driver.Client, tripId);
        clock.Set(at.AddMinutes(-100));
        var late = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/scheduled/{tripId}/release", new { })).ReadJsonAsync();
        Assert.Equal(3, late.GetProperty("penaltyPoints").GetInt32());
        Assert.True((await SchedulingFlow.ReservationAsync(fixture, tripId)).IsLateRelease);
        Assert.Equal(3, (await ReliabilityAsync(driver)).GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await driver.Client.PostAsJsonAsync($"/api/v1/driver/scheduled/{tripId}/release", new { })).StatusCode);
    }
}
