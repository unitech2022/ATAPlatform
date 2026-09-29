using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Matching;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class FavoriteMatchingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const decimal TwoKm = 0.018m;

    [Fact]
    public async Task Exclusive_offer_to_the_favorite_accepted_discounts_fare_receipt_and_ledger_while_driver_earnings_stay_unchanged()
    {
        var area = TripFlow.Area(0);
        using var admin = await fixture.LoginAdminAsync();
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        // A closer rival: without the exclusive round it would receive the offer.
        var (rival, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "بندر الغامدي");
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat + TwoKm, area.Lng);
        await TripFlow.TopupAsync(passenger.Client, 200m);

        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, favoriteDriverId: driverId))).ReadJsonAsync();
        var quoteId = quote.GetProperty("quoteId").GetString();
        var quoteRow = await fixture.Factory.WithDbAsync(db => db.FareQuotes.AsNoTracking().SingleAsync(q => q.Id == Guid.Parse(quoteId!)));
        var expectedDiscount = Math.Min(TripFlow.Round2(quoteRow.BaseAmount * 0.10m), 10m);

        var created = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId, "wallet", quoteId: quoteId));
        var tripId = created.GetProperty("id").GetString()!;
        Assert.Equal("requested", created.GetProperty("favorite").GetProperty("status").GetString());
        Assert.False(created.GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
        Assert.Equal("محمد", created.GetProperty("favorite").GetProperty("driverName").GetString());
        Assert.Equal(quoteRow.Total, created.GetProperty("estimatedFare").GetDecimal());

        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        Assert.True(offer.GetProperty("isFavoriteRequest").GetBoolean());
        Assert.True(offer.GetProperty("exclusive").GetBoolean());
        Assert.Equal(0, offer.GetProperty("round").GetInt32());
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddSeconds(30), offer.GetProperty("expiresAt").GetDateTime().ToUniversalTime());
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(rival.Client)).ValueKind);
        // The rest of the pass does nothing while the exclusive offer is open.
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(rival.Client)).ValueKind);

        var rounds = (await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync()).GetProperty("attempts");
        var round = Assert.Single(rounds.EnumerateArray());
        Assert.Equal(0, round.GetProperty("round").GetInt32());
        Assert.Equal("favorite", round.GetProperty("mode").GetString());
        Assert.Equal(1, round.GetProperty("candidatesCount").GetInt32());

        var accepted = await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        // The driver's copy of the trip (also pushed as TripUpdated) carries the favourite status but never the admin-only rule details.
        var driverTrip = await accepted.ReadJsonAsync();
        Assert.Equal("accepted", driverTrip.GetProperty("favorite").GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, driverTrip.GetProperty("favorite").GetProperty("discountRuleId").ValueKind);
        Assert.False(driverTrip.GetProperty("driver").GetProperty("isFavorite").GetBoolean());
        var passengerView = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.Equal("accepted", passengerView.GetProperty("favorite").GetProperty("status").GetString());
        Assert.True(passengerView.GetProperty("driver").GetProperty("isFavorite").GetBoolean());
        Assert.False(passengerView.GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
        // Once the favourite accepted, the trip is priced with the discount.
        Assert.Equal(TripFlow.RoundToHalf(quoteRow.BaseAmount - expectedDiscount), passengerView.GetProperty("estimatedFare").GetDecimal());
        var pinned = await fixture.Factory.WithDbAsync(db => db.Trips.AsNoTracking().Select(t => new { t.Id, t.FavoriteStatus, t.FavoriteDiscountRuleId }).SingleAsync(t => t.Id == Guid.Parse(tripId)));
        Assert.Equal(FavoriteStatus.Accepted, pinned.FavoriteStatus);
        Assert.NotNull(pinned.FavoriteDiscountRuleId);

        var pin = passengerView.GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver.Client, tripId, pin);
        Assert.Equal(HttpStatusCode.OK, (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).StatusCode);
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        var (core, baseFare) = FavoritesFlow.FareOf(trip);
        var discount = Math.Min(TripFlow.Round2(baseFare * 0.10m), 10m);
        Assert.True(discount > 0);
        Assert.Equal(discount, trip.DiscountTotal);
        Assert.Equal(TripFlow.RoundToHalf(baseFare - discount), trip.FinalFare);
        // Driver earnings are computed before the discount (80 % of the core), the platform bears the discount.
        Assert.Equal(TripFlow.Round2(core * 0.80m), trip.DriverEarnings);
        var breakdown = JsonDocument.Parse(trip.FareBreakdown!).RootElement;
        Assert.Equal(discount, breakdown.GetProperty("discount").GetDecimal());
        var stored = Assert.Single(breakdown.GetProperty("discounts").EnumerateArray());
        Assert.Equal("favorite_driver", stored.GetProperty("source").GetString());
        Assert.Equal(discount, stored.GetProperty("amount").GetDecimal());

        var view = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.True(view.GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
        Assert.Equal(JsonValueKind.Null, view.GetProperty("promotion").ValueKind);
        Assert.Equal(discount, view.GetProperty("discountTotal").GetDecimal());

        var receipt = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        var line = Assert.Single(receipt.GetProperty("lines").EnumerateArray(), l => l.GetProperty("code").GetString() == "discount");
        Assert.Equal("favorite_driver", line.GetProperty("source").GetString());
        Assert.Equal(-discount, line.GetProperty("amount").GetDecimal());
        Assert.Equal("خصم الكابتن المفضل", line.GetProperty("label").GetString());
        Assert.Equal(discount, receipt.GetProperty("discountTotal").GetDecimal());
        Assert.Equal(trip.FinalFare, receipt.GetProperty("lines").EnumerateArray().Sum(l => l.GetProperty("amount").GetDecimal()));
        using var english = fixture.CreateClient(passenger.Token, "en");
        var receiptEn = await (await english.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        Assert.Equal("Favourite driver discount", Assert.Single(receiptEn.GetProperty("lines").EnumerateArray(), l => l.GetProperty("code").GetString() == "discount").GetProperty("label").GetString());

        // Ledger: the passenger pays the discounted fare, the driver keeps the full share, discount_favorite_driver → trip_revenue is booked.
        Assert.Equal(200m - trip.FinalFare!.Value, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        var journal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().SingleAsync(j => j.ReferenceId == trip.Id && j.Type == JournalType.TripDiscount));
        Assert.Equal($"trip:{trip.Id}:discount:favorite_driver", journal.IdempotencyKey);
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.JournalId == journal.Id).ToListAsync());
        Assert.Equal(discount, entries.Single(e => e.Account == LedgerAccounts.DiscountFavoriteDriver).Debit);
        Assert.Equal(discount, entries.Single(e => e.Account == LedgerAccounts.TripRevenue).Credit);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}")).ReadJsonAsync();
        var adminFavorite = detail.GetProperty("favorite");
        Assert.Equal("accepted", adminFavorite.GetProperty("status").GetString());
        Assert.True(adminFavorite.GetProperty("discountApplied").GetBoolean());
        Assert.Equal("خصم الكابتن المفضل", adminFavorite.GetProperty("discountRuleName").GetString());
        Assert.Equal(pinned.FavoriteDiscountRuleId.ToString(), adminFavorite.GetProperty("discountRuleId").GetString());
        Assert.Equal("favorite_driver", detail.GetProperty("discounts")[0].GetProperty("source").GetString());
        Assert.Contains(detail.GetProperty("events").EnumerateArray(), e => e.GetProperty("type").GetString() == "favorite_accepted");
    }

    [Fact]
    public async Task Rejected_exclusive_offer_falls_back_to_normal_matching_without_reoffering_the_favorite_and_without_discount()
    {
        var area = TripFlow.Area(1);
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var (rival, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "بندر الغامدي");
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat + TwoKm, area.Lng);

        var tripId = (await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId))).GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.True(offer.GetProperty("exclusive").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await driver.Client.PostAsJsonAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/reject", new { reasonCode = "busy" })).StatusCode);

        // The next pass falls back: favorite_status = rejected, a normal round 1 offers the rival, the favourite is not asked again.
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("rejected", (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("status").GetString());
        var fallback = await FavoritesFlow.ActiveOfferAsync(rival.Client);
        Assert.Equal(JsonValueKind.Object, fallback.ValueKind);
        Assert.False(fallback.GetProperty("isFavoriteRequest").GetBoolean());
        Assert.False(fallback.GetProperty("exclusive").GetBoolean());
        Assert.Equal(1, fallback.GetProperty("round").GetInt32());
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(driver.Client)).ValueKind);
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.TripOffers.CountAsync(o => o.TripId == Guid.Parse(tripId) && o.DriverId == driverId)));
        var attempts = await fixture.Factory.WithDbAsync(db => db.MatchingAttempts.AsNoTracking().Where(a => a.TripId == Guid.Parse(tripId)).OrderBy(a => a.Round).ToListAsync());
        Assert.Equal([MatchingMode.Favorite, MatchingMode.Normal], attempts.Select(a => a.Mode).ToArray());
        Assert.All(attempts.Take(1), a => Assert.Equal(MatchingOutcome.Exhausted, a.Outcome));

        Assert.Equal(HttpStatusCode.OK, (await rival.Client.PostAsync($"/api/v1/driver/offers/{fallback.GetProperty("id").GetString()}/accept", null)).StatusCode);
        Assert.False((await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("driver").GetProperty("isFavorite").GetBoolean());
        // push trip.favorite_fallback names the replacement driver.
        var deliveries = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AsNoTracking().Where(d => d.UserId == passenger.UserId && d.EventCode == NotificationTypes.TripFavoriteFallback).ToListAsync());
        var push = Assert.Single(deliveries);
        Assert.Contains("بندر", JsonDocument.Parse(push.Payload).RootElement.GetProperty("contents").GetProperty("ar").GetString());

        var pin = (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(rival.Client, tripId, pin);
        (await rival.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(0m, trip.DiscountTotal);
        Assert.Null(trip.FavoriteDiscountRuleId);
        Assert.Equal(FavoriteStatus.Rejected, trip.FavoriteStatus);
        Assert.Empty(JsonDocument.Parse(trip.FareBreakdown!).RootElement.GetProperty("discounts").EnumerateArray());
        Assert.False(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AnyAsync(j => j.ReferenceId == trip.Id && j.Type == JournalType.TripDiscount)));
        var view = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.Equal("rejected", view.GetProperty("favorite").GetProperty("status").GetString());
        Assert.False(view.GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
        var receipt = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        Assert.DoesNotContain(receipt.GetProperty("lines").EnumerateArray(), l => l.GetProperty("code").GetString() == "discount");
    }

    [Fact]
    public async Task Expired_exclusive_offer_falls_back_and_the_total_search_timeout_only_starts_after_the_exclusive_round()
    {
        var area = TripFlow.Area(2);
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var clock = fixture.Factory.Clock;

        var tripId = (await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId))).GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        Assert.True((await FavoritesFlow.ActiveOfferAsync(driver.Client)).GetProperty("exclusive").GetBoolean());

        // 29 s: still exclusive. 31 s: expired → expired status, normal rounds (the only driver is excluded: nobody is found).
        clock.Advance(TimeSpan.FromSeconds(29));
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("requested", (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("status").GetString());
        clock.Advance(TimeSpan.FromSeconds(2));
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat, area.Lng);
        await fixture.Factory.RunMatcherAsync();
        var afterExpiry = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.Equal("expired", afterExpiry.GetProperty("favorite").GetProperty("status").GetString());
        Assert.Equal("searching", afterExpiry.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(driver.Client)).ValueKind);
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.TripOffers.CountAsync(o => o.TripId == Guid.Parse(tripId))));

        // 131 s after the request (100 s after the exclusive round ended): the default 120 s search timeout would have fired without F16.
        clock.Advance(TimeSpan.FromSeconds(100));
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat, area.Lng);
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("searching", (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("status").GetString());
        // 121 s after the exclusive round ended → no_drivers.
        clock.Advance(TimeSpan.FromSeconds(21));
        await fixture.Factory.RunMatcherAsync();
        var final = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.Equal("no_drivers", final.GetProperty("status").GetString());
        Assert.Equal("expired", final.GetProperty("favorite").GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unavailable_favorite_matches_normally_at_once_and_is_promoted_to_accepted_when_the_favorite_takes_the_trip_later()
    {
        var area = TripFlow.Area(3);
        using var admin = await fixture.LoginAdminAsync();
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var (rival, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "بندر الغامدي");
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();

        // Offline at request time: unavailable at once, and the very next pass offers the trip to the rival (no exclusive round).
        var created = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId));
        var tripId = created.GetProperty("id").GetString()!;
        Assert.Equal("unavailable", created.GetProperty("favorite").GetProperty("status").GetString());
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(rival.Client);
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        Assert.False(offer.GetProperty("isFavoriteRequest").GetBoolean());
        Assert.Equal(1, offer.GetProperty("round").GetInt32());
        var rounds = await fixture.Factory.WithDbAsync(db => db.MatchingAttempts.AsNoTracking().Where(a => a.TripId == Guid.Parse(tripId)).ToListAsync());
        Assert.DoesNotContain(rounds, a => a.Mode == MatchingMode.Favorite);
        Assert.Equal(HttpStatusCode.OK, (await rival.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).StatusCode);
        var pin = (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(rival.Client, tripId, pin);
        (await rival.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var normal = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(FavoriteStatus.Unavailable, normal.FavoriteStatus);
        Assert.Equal(0m, normal.DiscountTotal);

        // The zone/category can switch the exclusive round off (prefer_favorite_driver = false): unavailable although the favourite is online.
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = area.Lat, longitude = area.Lng })).EnsureSuccessStatusCode();
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat, area.Lng);
        var settings = (await (await admin.GetAsync("/api/v1/admin/matching-settings")).ReadJsonAsync()).EnumerateArray().First();
        var settingsId = settings.GetProperty("id").GetString();
        (await admin.PutAsJsonAsync($"/api/v1/admin/matching-settings/{settingsId}", new { preferFavoriteDriver = false })).EnsureSuccessStatusCode();
        var noExclusive = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId));
        Assert.Equal("unavailable", noExclusive.GetProperty("favorite").GetProperty("status").GetString());
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{noExclusive.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/v1/admin/matching-settings/{settingsId}", new { preferFavoriteDriver = true })).EnsureSuccessStatusCode();

        // Unavailable at the request, but later the favourite is the one matched: the trip becomes accepted with the discount.
        (await rival.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        var later = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId));
        var laterId = later.GetProperty("id").GetString()!;
        Assert.Equal("unavailable", later.GetProperty("favorite").GetProperty("status").GetString());
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = area.Lat, longitude = area.Lng })).EnsureSuccessStatusCode();
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat, area.Lng);
        await fixture.Factory.RunMatcherAsync();
        var normalOffer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.True(normalOffer.GetProperty("isFavoriteRequest").GetBoolean());
        Assert.False(normalOffer.GetProperty("exclusive").GetBoolean());
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{normalOffer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        Assert.Equal("accepted", (await FavoritesFlow.TripAsync(passenger.Client, laterId)).GetProperty("favorite").GetProperty("status").GetString());
        // Also matched normally, the assigned favourite is flagged `driver.isFavorite` for the rider.
        Assert.True((await FavoritesFlow.TripAsync(passenger.Client, laterId)).GetProperty("driver").GetProperty("isFavorite").GetBoolean());
        var laterPin = (await FavoritesFlow.TripAsync(passenger.Client, laterId)).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver.Client, laterId, laterPin);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{laterId}/complete", new { })).EnsureSuccessStatusCode();
        Assert.True((await RewardsFlow.TripAsync(fixture, laterId)).DiscountTotal > 0);
    }

    [Fact]
    public async Task Scheduled_trip_gets_the_exclusive_round_when_its_search_starts()
    {
        var area = TripFlow.Area(4);
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var clock = fixture.Factory.Clock;
        var (rival, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "بندر الغامدي");

        var scheduledAt = clock.UtcNow.AddMinutes(40);
        var created = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId, bookingType: "scheduled", scheduledAt: scheduledAt));
        var tripId = created.GetProperty("id").GetString()!;
        // Not eligible-checked at request time for scheduled trips: still requested, and nobody is searched for before T − lead.
        Assert.Equal("requested", created.GetProperty("favorite").GetProperty("status").GetString());
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(driver.Client)).ValueKind);

        clock.Advance(TimeSpan.FromMinutes(26));
        await FavoritesFlow.MoveDriverAsync(driver.Client, area.Lat + TwoKm, area.Lng);
        await FavoritesFlow.MoveDriverAsync(rival.Client, area.Lat, area.Lng);
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        Assert.True(offer.GetProperty("exclusive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, (await FavoritesFlow.ActiveOfferAsync(rival.Client)).ValueKind);
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        Assert.Equal("accepted", (await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("status").GetString());
    }
}
