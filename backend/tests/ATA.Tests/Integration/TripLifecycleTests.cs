using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class TripLifecycleTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Full_lifecycle_with_wallet_payment_assigns_nearest_driver_and_settles_ledger()
    {
        var area = TripFlow.Area(0);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var passengerUserId = Guid.Parse(passengerAuth.GetProperty("user").GetProperty("id").GetString()!);
        await TripFlow.TopupAsync(passenger, 200m);

        var (far, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, far, "سائق بعيد");
        await TripFlow.GoOnlineAsync(far, area.Lat + 0.02m, area.Lng + 0.01m);
        var (near, nearAuth) = await fixture.LoginAsync("driver");
        var nearId = await TripFlow.ApproveDriverAsync(fixture, near, "سائق قريب");
        await TripFlow.GoOnlineAsync(near, area.Lat + 0.001m, area.Lng + 0.001m);

        var estimate = await passenger.PostAsJsonAsync("/api/v1/passenger/trips/estimate", TripFlow.Route(area));
        Assert.Equal(HttpStatusCode.OK, estimate.StatusCode);
        var estimateBody = await estimate.ReadJsonAsync();
        Assert.True(estimateBody.GetProperty("distanceMeters").GetInt32() > 5000);
        // F10: the estimate is an alias of POST /pricing/quote (per-category `total`, breakdown and offer bounds).
        var economy = estimateBody.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        Assert.True(economy.GetProperty("total").GetDecimal() > 12m);
        Assert.True(economy.GetProperty("driverNetEarnings").GetDecimal() < economy.GetProperty("total").GetDecimal());
        Assert.Equal(1, economy.GetProperty("etaMinutes").GetInt32());

        var created = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, paymentMethod: "wallet"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trip = await created.ReadJsonAsync();
        var tripId = trip.GetProperty("id").GetString()!;
        Assert.Equal("searching", trip.GetProperty("status").GetString());
        Assert.StartsWith("T-20260928-", trip.GetProperty("tripNumber").GetString());
        Assert.Equal(JsonValueKind.Null, trip.GetProperty("pin").ValueKind);
        Assert.Equal(JsonValueKind.Null, trip.GetProperty("driver").ValueKind);
        Assert.Equal(economy.GetProperty("total").GetDecimal(), trip.GetProperty("estimatedFare").GetDecimal());

        var duplicate = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("trip_active_exists", await duplicate.ErrorCodeAsync());

        var active = await (await passenger.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync();
        Assert.Equal(tripId, active.GetProperty("id").GetString());

        Assert.True(await fixture.Factory.RunMatcherAsync() >= 1);
        var farOffer = await far.GetAsync("/api/v1/driver/offers/active");
        Assert.Equal(JsonValueKind.Null, (await farOffer.ReadJsonAsync()).ValueKind);
        var offer = await (await near.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(tripId, offer.GetProperty("tripId").GetString());
        Assert.True(offer.GetProperty("distanceToPickupMeters").GetInt32() < 300);
        Assert.Equal(economy.GetProperty("total").GetDecimal(), offer.GetProperty("passengerPrice").GetDecimal());

        var accepted = await near.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var assigned = await accepted.ReadJsonAsync();
        Assert.Equal("driver_assigned", assigned.GetProperty("status").GetString());
        Assert.Equal(nearId.ToString(), assigned.GetProperty("driver").GetProperty("id").GetString());
        Assert.Equal(JsonValueKind.Null, assigned.GetProperty("pin").ValueKind);
        Assert.Equal("Camry", assigned.GetProperty("vehicle").GetProperty("model").GetString());

        var passengerView = await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        var pin = passengerView.GetProperty("pin").GetString()!;
        Assert.Matches("^[0-9]{4}$", pin);
        var masked = passengerView.GetProperty("driver").GetProperty("phoneMasked").GetString()!;
        Assert.Contains("****", masked);
        Assert.StartsWith("+9665", masked);
        Assert.Equal(5m, passengerView.GetProperty("driver").GetProperty("ratingAvg").GetDecimal());

        Assert.Equal("driver_en_route", (await (await near.PostAsync($"/api/v1/driver/trips/{tripId}/en-route", null)).ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await near.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat + 0.0005m, lng = area.Lng, heading = 90, speed = 12.5 })).StatusCode);
        var arrived = await (await near.PostAsync($"/api/v1/driver/trips/{tripId}/arrived", null)).ReadJsonAsync();
        Assert.Equal("waiting", arrived.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, arrived.GetProperty("timeline").GetProperty("arrivedAt").ValueKind);

        var wrongPin = pin == "0000" ? "1111" : "0000";
        var wrong = await near.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/verify-pin", new { pin = wrongPin });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        var wrongBody = await wrong.ReadJsonAsync();
        Assert.Equal("pin_invalid", wrongBody.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(4, wrongBody.GetProperty("error").GetProperty("details").GetProperty("attemptsLeft").GetInt32());

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var verified = await near.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/verify-pin", new { pin });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var verifiedBody = await verified.ReadJsonAsync();
        Assert.Equal("pin_verified", verifiedBody.GetProperty("status").GetString());
        Assert.Equal(120, verifiedBody.GetProperty("waitingSeconds").GetInt32());

        Assert.Equal("in_trip", (await (await near.PostAsync($"/api/v1/driver/trips/{tripId}/start", null)).ReadJsonAsync()).GetProperty("status").GetString());
        var tooEarlyCancel = await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" });
        Assert.Equal(HttpStatusCode.Conflict, tooEarlyCancel.StatusCode);

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(20));
        var completed = await near.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { finalLat = area.Lat + 0.05m, finalLng = area.Lng + 0.05m });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var completedBody = await completed.ReadJsonAsync();
        Assert.Equal("completed", completedBody.GetProperty("status").GetString());
        Assert.Equal("wallet", completedBody.GetProperty("paymentMethod").GetString());
        var fare = completedBody.GetProperty("finalFare").GetDecimal();
        Assert.True(fare > trip.GetProperty("estimatedFare").GetDecimal(), "waiting time is billed");

        var dbTrip = await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == Guid.Parse(tripId)));
        // F10: total = round(core + booking fee, 0.5); the driver's net is 80% of the core (subtotal × multipliers), so the booking fee is not shared.
        var core = TripFlow.EconomyCore(dbTrip.FinalDistanceM!.Value, dbTrip.FinalDurationS!.Value, dbTrip.WaitingSeconds);
        Assert.Equal(TripFlow.RoundToHalf(core + 2m), fare);
        Assert.Equal(decimal.Round(core * 0.8m, 2), dbTrip.DriverEarnings);
        var passengerBalance = await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.UserId == passengerUserId && w.Kind == WalletKind.Passenger).Select(w => w.Balance).FirstAsync());
        Assert.Equal(200m - fare, passengerBalance);
        var nearUserId = Guid.Parse(nearAuth.GetProperty("user").GetProperty("id").GetString()!);
        var driverBalance = await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.UserId == nearUserId && w.Kind == WalletKind.Driver).Select(w => w.Balance).FirstAsync());
        Assert.Equal(dbTrip.DriverEarnings, driverBalance);

        var transactions = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.Where(t => t.ReferenceId == Guid.Parse(tripId)).ToListAsync());
        Assert.Equal(2, transactions.Count);
        var transactionIds = transactions.Select(t => (Guid?)t.Id).ToList();
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.Where(e => transactionIds.Contains(e.TransactionId)).ToListAsync());
        Assert.Equal(4, entries.Count);
        Assert.Equal(entries.Sum(e => e.Debit), entries.Sum(e => e.Credit));
        Assert.Equal(fare - dbTrip.DriverEarnings, entries.Where(e => e.Account == LedgerAccounts.TripRevenue).Sum(e => e.Credit - e.Debit));

        var events = await fixture.Factory.WithDbAsync(db => db.TripEvents.Where(e => e.TripId == Guid.Parse(tripId)).OrderBy(e => e.CreatedAt).Select(e => e.Type).ToListAsync());
        Assert.Equal(["requested", "search_started", "offer_sent", "offer_accepted", "driver_assigned", "driver_en_route", "driver_arrived", "waiting_started", "pin_failed", "pin_verified", "started", "payment_recorded", "completed"], events);
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.DriverLocationHistory.CountAsync(h => h.TripId == Guid.Parse(tripId))));
        Assert.Null(await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == nearId).Select(d => d.CurrentTripId).FirstAsync()));
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == nearId).Select(d => d.AcceptanceCount).FirstAsync()));

        var notificationTypes = await fixture.Factory.WithDbAsync(db => db.Notifications.Where(n => n.UserId == passengerUserId).Select(n => n.Type).ToListAsync());
        Assert.Contains(NotificationTypes.TripDriverAssigned, notificationTypes);
        Assert.Contains(NotificationTypes.TripDriverArrived, notificationTypes);
        Assert.Contains(NotificationTypes.TripCompleted, notificationTypes);

        var summary = await (await near.GetAsync("/api/v1/driver/earnings/summary")).ReadJsonAsync();
        Assert.Equal(1, summary.GetProperty("today").GetProperty("trips").GetInt32());
        Assert.Equal(dbTrip.DriverEarnings, summary.GetProperty("today").GetProperty("earnings").GetDecimal());
        Assert.Equal(dbTrip.DriverEarnings, summary.GetProperty("week").GetProperty("earnings").GetDecimal());

        var driverTrips = await (await near.GetAsync("/api/v1/driver/trips?status=completed")).ReadJsonAsync();
        Assert.Equal(1, driverTrips.GetProperty("total").GetInt32());
        Assert.Equal(dbTrip.DriverEarnings, driverTrips.GetProperty("items")[0].GetProperty("earning").GetDecimal());
        var passengerTrips = await (await passenger.GetAsync("/api/v1/passenger/trips?status=completed")).ReadJsonAsync();
        Assert.Equal(1, passengerTrips.GetProperty("total").GetInt32());
        Assert.Equal("completed", passengerTrips.GetProperty("items")[0].GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, (await (await near.GetAsync("/api/v1/driver/trips/active")).ReadJsonAsync()).ValueKind);
        Assert.Equal(JsonValueKind.Null, (await (await passenger.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Wallet_without_balance_falls_back_to_cash_and_credits_driver_against_cash_collected()
    {
        var area = TripFlow.Area(1);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, driver, "سائق نقدي");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, TripFlow.Request(area, paymentMethod: "wallet"));
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString();
        await TripFlow.DriveAsync(driver, tripId, pin!);

        var completed = await (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync();
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        Assert.Equal("cash", completed.GetProperty("paymentMethod").GetString());
        var fare = completed.GetProperty("finalFare").GetDecimal();

        var events = await fixture.Factory.WithDbAsync(db => db.TripEvents.Where(e => e.TripId == Guid.Parse(tripId)).Select(e => e.Type).ToListAsync());
        Assert.Contains("payment_fallback_cash", events);
        var driverUserId = Guid.Parse(driverAuth.GetProperty("user").GetProperty("id").GetString()!);
        var transactions = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.Where(t => t.ReferenceId == Guid.Parse(tripId)).ToListAsync());
        // F11: a cash trip credits the driver share (trip_earning) and then debits the whole cash fare (cash_collection, overdraft allowed).
        Assert.Equal(2, transactions.Count);
        var earning = Assert.Single(transactions, t => t.Type == TransactionType.TripEarning);
        var collection = Assert.Single(transactions, t => t.Type == TransactionType.CashCollection);
        var dbTrip = await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == Guid.Parse(tripId)));
        Assert.Equal(dbTrip.DriverEarnings, earning.Amount);
        Assert.Equal(fare, collection.Amount);
        var core = TripFlow.EconomyCore(dbTrip.FinalDistanceM!.Value, dbTrip.FinalDurationS!.Value, dbTrip.WaitingSeconds);
        Assert.Equal(TripFlow.RoundToHalf(core + 2m), fare);
        Assert.Equal(decimal.Round(core * 0.8m, 2), earning.Amount);
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.Where(e => e.TransactionId == earning.Id || e.TransactionId == collection.Id).ToListAsync());
        Assert.Contains(entries, e => e.Account == LedgerAccounts.CashCollected && e.Debit == earning.Amount);
        Assert.Contains(entries, e => e.Account == LedgerAccounts.CashCollected && e.Credit == fare);
        Assert.Equal(entries.Sum(e => e.Debit), entries.Sum(e => e.Credit));
        var balance = await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.UserId == driverUserId && w.Kind == WalletKind.Driver).Select(w => w.Balance).FirstAsync());
        Assert.Equal(earning.Amount - fare, balance);
        Assert.Null(await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.CurrentTripId).FirstAsync()));
    }

    [Fact]
    public async Task Female_driver_preference_skips_nearer_male_driver()
    {
        var area = TripFlow.Area(2);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (male, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, male, "سائق", "male");
        await TripFlow.GoOnlineAsync(male, area.Lat, area.Lng);
        var (female, _) = await fixture.LoginAsync("driver");
        var femaleId = await TripFlow.ApproveDriverAsync(fixture, female, "سائقة", "female");
        await TripFlow.GoOnlineAsync(female, area.Lat + 0.02m, area.Lng);

        var created = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, preferFemaleDriver: true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await fixture.Factory.RunMatcherAsync();

        Assert.Equal(JsonValueKind.Null, (await (await male.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);
        var offer = await (await female.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        var accepted = await (await female.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).ReadJsonAsync();
        Assert.Equal(femaleId.ToString(), accepted.GetProperty("driver").GetProperty("id").GetString());
        Assert.Equal("female", accepted.GetProperty("driver").GetProperty("gender").GetString());
    }

    [Fact]
    public async Task Passenger_cancels_before_assignment_and_can_request_again()
    {
        var area = TripFlow.Area(3);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString();

        var missingReason = await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { note = "x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingReason.StatusCode);
        // F14: the reason code must be an active, selectable passenger reason of the catalogue.
        var unknownReason = await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_plans" });
        Assert.Equal("cancellation_reason_invalid", await unknownReason.ErrorCodeAsync());

        var cancelled = await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind", note = "لاحقاً" });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var body = await cancelled.ReadJsonAsync();
        Assert.Equal("cancelled", body.GetProperty("status").GetString());
        Assert.Equal("passenger", body.GetProperty("cancelledBy").GetString());
        Assert.Equal("changed_mind", body.GetProperty("cancellationReason").GetString());
        Assert.Contains(body.GetProperty("events").EnumerateArray(), e => e.GetProperty("type").GetString() == "cancelled" && e.GetProperty("actor").GetString() == "passenger");

        var again = await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(JsonValueKind.Null, (await (await passenger.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync()).ValueKind);
        Assert.Equal(HttpStatusCode.Created, (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).StatusCode);

        var list = await (await passenger.GetAsync("/api/v1/passenger/trips?status=cancelled")).ReadJsonAsync();
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        var invalid = await passenger.GetAsync("/api/v1/passenger/trips?status=bogus");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
    }

    [Fact]
    public async Task Search_timeout_without_drivers_marks_trip_no_drivers()
    {
        var area = TripFlow.Area(4);
        var (passenger, auth) = await fixture.LoginAsync("passenger");
        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString();

        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("searching", (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("status").GetString());

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        await fixture.Factory.RunMatcherAsync();
        var trip = await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal("no_drivers", trip.GetProperty("status").GetString());
        Assert.Equal("system", trip.GetProperty("cancelledBy").GetString());

        var userId = Guid.Parse(auth.GetProperty("user").GetProperty("id").GetString()!);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == userId && n.Type == NotificationTypes.TripNoDrivers)));
        Assert.Equal(JsonValueKind.Null, (await (await passenger.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Expired_offer_moves_to_next_driver_and_rejection_is_counted()
    {
        var area = TripFlow.Area(5);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (first, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, first, "الأول");
        await TripFlow.GoOnlineAsync(first, area.Lat, area.Lng);
        var (second, _) = await fixture.LoginAsync("driver");
        var secondId = await TripFlow.ApproveDriverAsync(fixture, second, "الثاني");
        await TripFlow.GoOnlineAsync(second, area.Lat + 0.01m, area.Lng);

        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = Guid.Parse(created.GetProperty("id").GetString()!);
        await fixture.Factory.RunMatcherAsync();
        var firstOffer = await (await first.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, firstOffer.ValueKind);
        Assert.Equal(JsonValueKind.Null, (await (await second.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(21));
        Assert.Equal(JsonValueKind.Null, (await (await first.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);
        var late = await first.PostAsync($"/api/v1/driver/offers/{firstOffer.GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
        Assert.Equal("offer_expired", await late.ErrorCodeAsync());

        await fixture.Factory.RunMatcherAsync();
        var secondOffer = await (await second.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, secondOffer.ValueKind);
        var offers = await fixture.Factory.WithDbAsync(db => db.TripOffers.Where(o => o.TripId == tripId).OrderBy(o => o.SentAt).Select(o => o.Status).ToListAsync());
        Assert.Equal([OfferStatus.Expired, OfferStatus.Sent], offers);

        var rejected = await second.PostAsJsonAsync($"/api/v1/driver/offers/{secondOffer.GetProperty("id").GetString()}/reject", new { reasonCode = "too_far" });
        Assert.Equal(HttpStatusCode.NoContent, rejected.StatusCode);
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == secondId).Select(d => d.RejectionCount).FirstAsync()));

        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("searching", await fixture.Factory.WithDbAsync(db => db.Trips.Where(t => t.Id == tripId).Select(t => t.Status.ToString().ToLowerInvariant()).FirstAsync()));
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.TripOffers.CountAsync(o => o.TripId == tripId)));
    }

    [Fact]
    public async Task Admin_lists_details_live_map_and_cancels_with_audit()
    {
        var area = TripFlow.Area(6);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, driver, "سائق الإدارة");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, TripFlow.Request(area));
        var tripId = trip.GetProperty("id").GetString()!;
        var tripNumber = trip.GetProperty("tripNumber").GetString()!;

        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/trips")).StatusCode);
        using var admin = await fixture.LoginAdminAsync();

        var list = await (await admin.GetAsync($"/api/v1/admin/trips?status=driver_assigned&search={tripNumber}&from=2026-09-28&to=2026-09-28")).ReadJsonAsync();
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        var item = list.GetProperty("items")[0];
        Assert.Equal("سائق الإدارة", item.GetProperty("driverName").GetString());
        Assert.Equal("اقتصادي", item.GetProperty("categoryName").GetString());
        Assert.StartsWith("+9665", item.GetProperty("passengerPhone").GetString());
        Assert.Equal(0, (await (await admin.GetAsync($"/api/v1/admin/trips?search={tripNumber}&to=2026-09-27")).ReadJsonAsync()).GetProperty("total").GetInt32());

        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal(tripNumber, detail.GetProperty("tripNumber").GetString());
        Assert.StartsWith("+9665", detail.GetProperty("passenger").GetProperty("phoneNumber").GetString());
        Assert.StartsWith("+9665", detail.GetProperty("driver").GetProperty("phoneNumber").GetString());
        Assert.DoesNotContain("*", detail.GetProperty("driver").GetProperty("phoneNumber").GetString());
        Assert.Contains(detail.GetProperty("events").EnumerateArray(), e => e.GetProperty("type").GetString() == "offer_accepted" && e.GetProperty("actorName").GetString() == "سائق الإدارة");
        Assert.Equal(1, detail.GetProperty("offers").GetArrayLength());
        Assert.False(detail.TryGetProperty("pin", out _));

        var live = await (await admin.GetAsync("/api/v1/admin/live")).ReadJsonAsync();
        var liveDriver = live.GetProperty("drivers").EnumerateArray().Single(d => d.GetProperty("driverId").GetString() == driverId.ToString());
        Assert.Equal("on_trip", liveDriver.GetProperty("status").GetString());
        Assert.Equal("economy", liveDriver.GetProperty("categoryCode").GetString());
        Assert.Equal(tripId, liveDriver.GetProperty("currentTripId").GetString());
        Assert.Contains(live.GetProperty("activeTrips").EnumerateArray(), t => t.GetProperty("id").GetString() == tripId);
        Assert.NotEqual(JsonValueKind.Null, live.GetProperty("generatedAt").ValueKind);

        var summary = await (await admin.GetAsync("/api/v1/admin/dashboard/summary")).ReadJsonAsync();
        Assert.True(summary.GetProperty("tripsToday").GetInt32() >= 1);

        var noReason = await admin.PostAsJsonAsync($"/api/v1/admin/trips/{tripId}/cancel", new { reason = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noReason.StatusCode);
        var cancelled = await admin.PostAsJsonAsync($"/api/v1/admin/trips/{tripId}/cancel", new { reason = "بلاغ من الراكب" });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var cancelledBody = await cancelled.ReadJsonAsync();
        Assert.Equal("cancelled", cancelledBody.GetProperty("status").GetString());
        Assert.Equal("admin", cancelledBody.GetProperty("cancelledBy").GetString());

        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "trip" && a.EntityId == Guid.Parse(tripId)).Select(a => a.Action).ToListAsync());
        Assert.Equal(["trip.cancel"], audits);
        Assert.Null(await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.CurrentTripId).FirstAsync()));
        var passengerUserId = Guid.Parse(passengerAuth.GetProperty("user").GetProperty("id").GetString()!);
        var driverUserId = Guid.Parse(driverAuth.GetProperty("user").GetProperty("id").GetString()!);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == passengerUserId && n.Type == NotificationTypes.TripCancelled)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driverUserId && n.Type == NotificationTypes.TripCancelled)));

        var idleDriver = (await (await admin.GetAsync("/api/v1/admin/live")).ReadJsonAsync()).GetProperty("drivers").EnumerateArray().Single(d => d.GetProperty("driverId").GetString() == driverId.ToString());
        Assert.Equal("idle", idleDriver.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, (await (await driver.GetAsync("/api/v1/driver/trips/active")).ReadJsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Hub_pushes_TripUpdated_and_OfferReceived_over_signalr()
    {
        var area = TripFlow.Area(7);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "سائق البث");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        await using var passengerHub = TripFlow.Hub(fixture, passengerAuth.GetProperty("accessToken").GetString()!);
        await using var driverHub = TripFlow.Hub(fixture, driverAuth.GetProperty("accessToken").GetString()!);
        var tripUpdates = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var offers = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        passengerHub.On<JsonElement>("TripUpdated", t => tripUpdates.TrySetResult(t));
        driverHub.On<JsonElement>("OfferReceived", o => offers.TrySetResult(o));
        await passengerHub.StartAsync();
        await driverHub.StartAsync();

        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var pushed = await tripUpdates.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.GetProperty("id").GetString(), pushed.GetProperty("id").GetString());
        Assert.Equal("searching", pushed.GetProperty("status").GetString());

        await fixture.Factory.RunMatcherAsync();
        var offer = await offers.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(created.GetProperty("id").GetString(), offer.GetProperty("tripId").GetString());

        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsync("/hubs/trips/negotiate?negotiateVersion=1", null)).StatusCode);
    }
}

/// <summary>Shared steps for trip tests: approved online drivers, trip requests and the driver-side transitions.</summary>
public static class TripFlow
{
    /// <summary>Distinct pickup areas (≈13 km apart, beyond the 12 km maximum matching radius) so drivers left online by one test never match another test's trip.</summary>
    public static (decimal Lat, decimal Lng) Area(int index) => (24.30m + 0.12m * index, 46.60m);

    public static object Route((decimal Lat, decimal Lng) area) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = SeedIds.RideCategories.Economy,
        bookingType = "now",
    };

    public static object Request((decimal Lat, decimal Lng) area, string paymentMethod = "cash", bool preferFemaleDriver = false, string? quoteId = null, string pricingMode = "fixed", decimal? offeredPrice = null) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = SeedIds.RideCategories.Economy,
        bookingType = "now",
        paymentMethod,
        preferFemaleDriver,
        pricingMode,
        offeredPrice,
        quoteId,
        riderNote = "بجانب البوابة",
    };

    public static async Task TopupAsync(HttpClient client, decimal amount)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/wallet/topups") { Content = JsonContent.Create(new { amount, method = "sandbox" }) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        (await client.SendAsync(request)).EnsureSuccessStatusCode();
    }

    public static async Task<Guid> ApproveDriverAsync(ApiFixture fixture, HttpClient driver, string name, string gender = "male")
    {
        var profile = await driver.PutAsJsonAsync("/api/v1/driver/application/profile", new
        {
            fullName = name, nationalId = "1012345678", dateOfBirth = "1990-05-20", cityId = SeedIds.CityRiyadh, gender,
        });
        profile.EnsureSuccessStatusCode();
        var vehicle = await driver.PutAsJsonAsync("/api/v1/driver/application/vehicle", new
        {
            make = "Toyota", model = "Camry", year = 2023, color = "White", plateNumber = $"TRP {fixture.NextPhone()[^4..]}", seats = 4, rideCategoryId = SeedIds.RideCategories.Economy,
        });
        vehicle.EnsureSuccessStatusCode();
        await DriverFlow.UploadAllDocumentsAsync(driver);
        (await driver.PostAsync("/api/v1/driver/application/submit", null)).EnsureSuccessStatusCode();
        var application = await (await driver.GetAsync("/api/v1/driver/application")).ReadJsonAsync();
        var number = application.GetProperty("applicationNumber").GetString();
        var driverId = await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.ApplicationNumber == number).Select(d => d.Id).FirstAsync());

        using var admin = await fixture.LoginAdminAsync();
        foreach (var documentId in DriverFlow.DocumentIds(application))
        {
            (await admin.PostAsJsonAsync($"/api/v1/admin/documents/{documentId}/verify", new { status = "verified" })).EnsureSuccessStatusCode();
        }

        (await admin.PostAsync($"/api/v1/admin/drivers/{driverId}/approve", null)).EnsureSuccessStatusCode();
        return driverId;
    }

    public static async Task GoOnlineAsync(HttpClient driver, decimal lat, decimal lng)
    {
        (await driver.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = lat, longitude = lng })).EnsureSuccessStatusCode();
        var location = await driver.PutAsJsonAsync("/api/v1/driver/location", new { lat, lng, heading = 0, accuracy = 5 });
        Assert.Equal(HttpStatusCode.NoContent, location.StatusCode);
    }

    /// <summary>Requests a trip, runs one matching pass and accepts the offer with <paramref name="driver"/>.</summary>
    public static async Task<JsonElement> RequestAndAssignAsync(ApiFixture fixture, HttpClient passenger, HttpClient driver, object request)
    {
        var created = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        var accepted = await driver.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        return await accepted.ReadJsonAsync();
    }

    /// <summary>en-route → arrived → verify-pin → start.</summary>
    public static async Task DriveAsync(HttpClient driver, string tripId, string pin)
    {
        (await driver.PostAsync($"/api/v1/driver/trips/{tripId}/en-route", null)).EnsureSuccessStatusCode();
        (await driver.PostAsync($"/api/v1/driver/trips/{tripId}/arrived", null)).EnsureSuccessStatusCode();
        (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/verify-pin", new { pin })).EnsureSuccessStatusCode();
        (await driver.PostAsync($"/api/v1/driver/trips/{tripId}/start", null)).EnsureSuccessStatusCode();
    }

    /// <summary>Seeded economy rule (8 + 1.8/km + 0.35/min, waiting 0.35/min, min 12) without time/demand multipliers.</summary>
    public static decimal EconomyCore(int distanceMeters, int durationSeconds, int waitingSeconds)
    {
        var subtotal = 8m + Round2(1.8m * distanceMeters / 1000m) + Round2(0.35m * durationSeconds / 60m) + Round2(0.35m * waitingSeconds / 60m);
        return Math.Max(subtotal, 12m);
    }

    public static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal RoundToHalf(decimal value) => decimal.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;

    public static HubConnection Hub(ApiFixture fixture, string accessToken) => new HubConnectionBuilder()
        .WithUrl(new Uri(fixture.Factory.Server.BaseAddress, $"hubs/trips?access_token={accessToken}"), o =>
        {
            o.HttpMessageHandlerFactory = _ => fixture.Factory.Server.CreateHandler();
            o.Transports = HttpTransportType.LongPolling;
        })
        .Build();
}
