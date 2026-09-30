using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Rider cancellation of a scheduled trip through the F14 engine (doc 11 §F17.3 "إلغاء الراكب لرحلة scheduled").</summary>
public class ScheduledCancellationTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Cancelling_is_free_until_T_minus_60_and_costs_the_late_fee_with_driver_compensation_after()
    {
        var area = TripFlow.Area(0);
        var clock = fixture.Factory.Clock;
        var at = clock.UtcNow.AddHours(3);
        var early = await SafetyFlow.PassengerAsync(fixture);
        var freeTrip = (await SchedulingFlow.BookAsync(early.Client, area, at)).GetProperty("id").GetString()!;

        var freePreview = await (await early.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{freeTrip}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("scheduled", freePreview.GetProperty("stage").GetString());
        Assert.Equal("scheduled", freePreview.GetProperty("bookingType").GetString());
        Assert.Equal(0m, freePreview.GetProperty("fee").GetDecimal());
        Assert.True(freePreview.GetProperty("isFree").GetBoolean());
        Assert.Equal(at.AddMinutes(-60), freePreview.GetProperty("freeUntil").GetDateTime().ToUniversalTime());
        var cancelledFree = await (await early.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{freeTrip}/cancel", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("cancelled", cancelledFree.GetProperty("status").GetString());
        Assert.Equal("scheduled", cancelledFree.GetProperty("cancellation").GetProperty("stage").GetString());
        Assert.Equal("none", cancelledFree.GetProperty("cancellation").GetProperty("atFault").GetString());
        var freeEvent = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(freeTrip)));
        Assert.Equal(0m, freeEvent.FeeCharged);
        Assert.False(freeEvent.CountsTowardRate);
        Assert.All(await SchedulingFlow.RemindersAsync(fixture, freeTrip), r => Assert.Equal(ReminderStatus.Cancelled, r.Status));

        // A driver holds the second booking; 30 minutes before pickup the rider pays the fixed 10 SAR and the driver gets 50 %.
        var late = await SafetyFlow.PassengerAsync(fixture);
        await TripFlow.TopupAsync(late.Client, 100m);
        var (driver, _) = await SchedulingFlow.DriverAsync(fixture, area);
        var lateTrip = (await SchedulingFlow.BookAsync(late.Client, area, at)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(driver.Client, lateTrip);
        clock.Set(at.AddMinutes(-61));
        Assert.True((await (await late.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateTrip}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync()).GetProperty("isFree").GetBoolean());
        clock.Set(at.AddMinutes(-60));
        Assert.True((await (await late.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateTrip}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync()).GetProperty("isFree").GetBoolean());
        clock.Set(at.AddMinutes(-30));
        var preview = await (await late.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateTrip}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal(10m, preview.GetProperty("fee").GetDecimal());
        Assert.False(preview.GetProperty("isFree").GetBoolean());
        Assert.Contains("10.00", preview.GetProperty("message").GetString());

        var changed = await late.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateTrip}/cancel", new { reasonCode = "changed_mind", expectedFee = 5m });
        Assert.Equal("cancellation_fee_changed", await changed.ErrorCodeAsync());
        var cancelled = await (await late.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{lateTrip}/cancel", new { reasonCode = "changed_mind", expectedFee = 10m })).ReadJsonAsync();
        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal("passenger", cancelled.GetProperty("cancellation").GetProperty("atFault").GetString());
        Assert.Equal(10m, cancelled.GetProperty("cancellation").GetProperty("feeCharged").GetDecimal());
        var lateEvent = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(lateTrip)));
        Assert.Equal(CancellationStage.Scheduled, lateEvent.Stage);
        Assert.Equal(BookingType.Scheduled, lateEvent.BookingType);
        Assert.True(lateEvent.CountsTowardRate);
        Assert.Equal(5m, lateEvent.CompensationAmount);
        Assert.Equal(90m, await SafetyFlow.WalletBalanceAsync(fixture, late.UserId, WalletKind.Passenger));
        Assert.Equal(5m, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        var reservation = await SchedulingFlow.ReservationAsync(fixture, lateTrip);
        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Equal(ReservationReleaseReason.TripCancelled, reservation.ReleaseReason);
        Assert.Equal(0, reservation.PenaltyPoints);
        Assert.Null((await SchedulingFlow.TripRowAsync(fixture, lateTrip)).ReservedDriverId);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, driver.UserId, NotificationTypes.TripCancelled));
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, driver.UserId, NotificationTypes.CancellationCompensation));
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, late.UserId, NotificationTypes.CancellationFeeCharged));
        Assert.All(await SchedulingFlow.RemindersAsync(fixture, lateTrip), r => Assert.NotEqual(ReminderStatus.Pending, r.Status));
        Assert.False(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(driver.Client), lateTrip));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Late_fee_follows_the_rule_type_and_a_trip_that_started_searching_uses_the_f14_stage_rules()
    {
        var area = TripFlow.Area(1);
        var clock = fixture.Factory.Clock;
        using var admin = await fixture.LoginAdminAsync();
        var rules = await (await admin.GetAsync("/api/v1/admin/scheduled-ride-rules")).ReadJsonAsync();
        var global = rules.EnumerateArray().Single(r => r.GetProperty("cityId").ValueKind == JsonValueKind.Null && r.GetProperty("rideCategoryId").ValueKind == JsonValueKind.Null);
        var ruleUrl = $"/api/v1/admin/scheduled-ride-rules/{global.GetProperty("id").GetString()}";
        var body = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(global.GetRawText())!;
        body.Remove("id"); body.Remove("createdAt"); body.Remove("updatedAt");
        void Set(string key, object? value) => body[key] = JsonSerializer.SerializeToElement(value);

        var at = clock.UtcNow.AddHours(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at)).GetProperty("id").GetString()!;
        var estimated = (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("estimatedFare").GetDecimal();
        clock.Set(at.AddMinutes(-20));

        Set("lateCancelFeeType", "percent"); Set("lateCancelFeePercent", 20m); Set("lateCancelFeeAmount", null);
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();
        var percent = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal(TripFlow.Round2(estimated * 0.2m), percent.GetProperty("fee").GetDecimal());

        Set("lateCancelFeeType", "none"); Set("lateCancelFeePercent", null);
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();
        var none = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal(0m, none.GetProperty("fee").GetDecimal());
        Assert.Equal("scheduled", none.GetProperty("stage").GetString());
        Assert.Equal(at.AddMinutes(-60), none.GetProperty("freeUntil").GetDateTime().ToUniversalTime());
        Assert.DoesNotContain("حتى", none.GetProperty("message").GetString());

        Set("lateCancelFeeType", "fixed"); Set("lateCancelFeeAmount", 10m); Set("freeCancelMinutesBefore", 15);
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();
        var widened = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.True(widened.GetProperty("isFree").GetBoolean());
        Assert.Equal(at.AddMinutes(-15), widened.GetProperty("freeUntil").GetDateTime().ToUniversalTime());
        Set("freeCancelMinutesBefore", 60);
        (await admin.PutAsJsonAsync(ruleUrl, body)).EnsureSuccessStatusCode();

        // Once the search started the trip is an ordinary searching trip: the F14 rules (before accept → free) apply.
        clock.Set(at.AddMinutes(-9));
        await fixture.Factory.RunScheduledWorkerAsync();
        var searching = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("before_accept", searching.GetProperty("stage").GetString());
        Assert.Equal(0m, searching.GetProperty("fee").GetDecimal());
    }
}

/// <summary>Card trips are only checked at booking and authorized when the search starts; a decline falls back to cash (doc 11 §F17.1 payment).</summary>
public class ScheduledPaymentTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Card_is_authorized_when_the_search_starts_not_at_booking()
    {
        var area = TripFlow.Area(2);
        var at = fixture.Factory.Clock.UtcNow.AddHours(2);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var cardId = await PaymentFlow.AddCardAsync(passenger.Client, "tok_sandbox_visa");
        var booked = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, at, "card", paymentMethodId: cardId));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var tripId = (await booked.ReadJsonAsync()).GetProperty("id").GetString()!;
        Assert.Equal(cardId, (await SchedulingFlow.TripRowAsync(fixture, tripId)).PaymentMethodId?.ToString());
        Assert.False(await fixture.Factory.WithDbAsync(db => db.Payments.AnyAsync(p => p.TripId == Guid.Parse(tripId))));

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-10));
        var payment = await fixture.Factory.WithDbAsync(db => db.Payments.AsNoTracking().SingleAsync(p => p.TripId == Guid.Parse(tripId)));
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Searching, row.Status);
        Assert.Equal(PaymentMethodKind.Card, row.PaymentMethod);
        Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.PaymentAuthorized));
    }

    [Fact]
    public async Task A_declined_authorization_falls_back_to_cash_with_a_payment_failed_notification()
    {
        var area = TripFlow.Area(3);
        var at = fixture.Factory.Clock.UtcNow.AddHours(2);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var cardId = await PaymentFlow.AddCardAsync(passenger.Client, "tok_sandbox_visa");
        var tripId = (await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", SchedulingFlow.Request(area, at, "card", paymentMethodId: cardId))).ReadJsonAsync()).GetProperty("id").GetString()!;
        // The issuer declines by the time the search starts.
        await fixture.Factory.WithDbAsync(async db =>
        {
            var card = await db.PaymentMethods.FirstAsync(m => m.Id == Guid.Parse(cardId));
            card.GatewayToken = "tok_sandbox_declined";
            await db.SaveChangesAsync();
            return true;
        });

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-10));
        var row = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.Equal(TripStatus.Searching, row.Status);
        Assert.Equal(PaymentMethodKind.Cash, row.PaymentMethod);
        Assert.Null(row.PaymentMethodId);
        var fallback = Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.PaymentFallbackCash));
        Assert.Contains("authorization_failed", fallback.Data);
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, passenger.UserId, NotificationTypes.PaymentFailed));
    }
}

/// <summary>Favourite-driver priority for scheduled rides (doc 11 §F17.3 step 2; the F16 hook).</summary>
public class ScheduledFavoriteTests(SchedulingFixture fixture) : IClassFixture<SchedulingFixture>
{
    [Fact]
    public async Task Favorite_gets_the_request_and_exclusive_marketplace_window_reserves_and_earns_the_discount_at_completion()
    {
        var area = TripFlow.Area(4);
        var clock = fixture.Factory.Clock;
        var (passenger, favorite, favoriteId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var (rival, _) = await SchedulingFlow.DriverAsync(fixture, area, "بندر الغامدي");
        var at = clock.UtcNow.AddHours(5);
        var booked = await SchedulingFlow.BookAsync(passenger.Client, area, at, favoriteDriverId: favoriteId);
        var tripId = booked.GetProperty("id").GetString()!;
        var requestedAt = clock.UtcNow;
        Assert.Equal("requested", booked.GetProperty("favorite").GetProperty("status").GetString());
        Assert.Single(await SchedulingFlow.NotificationsAsync(fixture, favorite.UserId, NotificationTypes.ScheduledFavoriteRequest));

        // Within `favorite_exclusive_minutes` only the favourite sees it and can reserve it.
        Assert.False(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(rival.Client), tripId));
        Assert.Equal(HttpStatusCode.NotFound, (await rival.Client.PostAsync($"/api/v1/driver/scheduled/{tripId}/reserve", null)).StatusCode);
        var mine = (await SchedulingFlow.MarketplaceAsync(favorite.Client)).GetProperty("items").EnumerateArray().Single(i => i.GetProperty("tripId").GetString() == tripId);
        Assert.True(mine.GetProperty("isFavoriteRequest").GetBoolean());
        Assert.Equal(requestedAt.AddMinutes(30), mine.GetProperty("exclusiveUntil").GetDateTime().ToUniversalTime());

        // After the window the market is open to everybody, and the favourite can still reserve (source = favorite).
        clock.Advance(TimeSpan.FromMinutes(31));
        await fixture.Factory.RunScheduledWorkerAsync();
        Assert.Single(await SchedulingFlow.EventsAsync(fixture, tripId, TripEventTypes.FavoriteWindowExpired));
        Assert.True(SchedulingFlow.Lists(await SchedulingFlow.MarketplaceAsync(rival.Client), tripId));
        var reserved = await SchedulingFlow.ReserveAsync(favorite.Client, tripId);
        Assert.Equal("favorite", reserved.GetProperty("source").GetString());
        Assert.Equal("requested", (await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("status").GetString());

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-60));
        await SchedulingFlow.ConfirmAsync(favorite.Client, tripId);
        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-15));
        await SchedulingFlow.ConfirmAsync(favorite.Client, tripId);
        var assigned = await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId);
        Assert.Equal("accepted", assigned.GetProperty("favorite").GetProperty("status").GetString());
        Assert.True(assigned.GetProperty("driver").GetProperty("isFavorite").GetBoolean());
        Assert.NotNull((await SchedulingFlow.TripRowAsync(fixture, tripId)).FavoriteDiscountRuleId);

        var pin = assigned.GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(favorite.Client, tripId, pin);
        (await favorite.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var done = await SchedulingFlow.TripRowAsync(fixture, tripId);
        Assert.True(done.DiscountTotal > 0m);
        Assert.True((await SchedulingFlow.PassengerTripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
    }

    [Fact]
    public async Task A_favorite_who_releases_the_reservation_is_unavailable_and_the_trip_goes_to_the_normal_search()
    {
        var area = TripFlow.Area(5);
        var clock = fixture.Factory.Clock;
        var (passenger, favorite, favoriteId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var at = clock.UtcNow.AddHours(5);
        var tripId = (await SchedulingFlow.BookAsync(passenger.Client, area, at, favoriteDriverId: favoriteId)).GetProperty("id").GetString()!;
        await SchedulingFlow.ReserveAsync(favorite.Client, tripId);
        (await favorite.Client.PostAsJsonAsync($"/api/v1/driver/scheduled/{tripId}/release", new { })).EnsureSuccessStatusCode();
        Assert.Equal(FavoriteStatus.Unavailable, (await SchedulingFlow.TripRowAsync(fixture, tripId)).FavoriteStatus);

        await SchedulingFlow.AtAsync(fixture, at.AddMinutes(-10));
        (await favorite.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(favorite.Client);
        Assert.False(offer.GetProperty("exclusive").GetBoolean());
        Assert.Equal(0, await fixture.Factory.WithDbAsync(db => db.MatchingAttempts.CountAsync(a => a.TripId == Guid.Parse(tripId) && a.Mode == ATA.Domain.Matching.MatchingMode.Favorite)));
    }
}
