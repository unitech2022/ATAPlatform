using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class CancellationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Passenger_fee_follows_the_stage_and_free_window_with_compensation_and_expected_fee_guard()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var searching = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var searchingId = searching.GetProperty("id").GetString();
        var freePreview = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{searchingId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("before_accept", freePreview.GetProperty("stage").GetString());
        Assert.Equal(0m, freePreview.GetProperty("fee").GetDecimal());
        Assert.True(freePreview.GetProperty("isFree").GetBoolean());
        var cancelledEarly = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{searchingId}/cancel", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("before_accept", cancelledEarly.GetProperty("cancellation").GetProperty("stage").GetString());
        Assert.Equal("none", cancelledEarly.GetProperty("cancellation").GetProperty("atFault").GetString());
        Assert.Equal("غيرت رأيي", cancelledEarly.GetProperty("cancellation").GetProperty("reasonName").GetString());
        var earlyEvent = await EventAsync(searchingId!);
        Assert.False(earlyEvent.CountsTowardRate);
        Assert.Equal(0m, earlyEvent.FeeCharged);

        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var ride = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        var assignedAt = fixture.Factory.Clock.UtcNow;
        var inWindow = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal("after_accept", inWindow.GetProperty("stage").GetString());
        Assert.True(inWindow.GetProperty("isFree").GetBoolean());
        Assert.Equal(assignedAt.AddSeconds(120), inWindow.GetProperty("freeUntil").GetDateTime().ToUniversalTime());

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        var charged = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/cancel/preview", new { reasonCode = "changed_mind" })).ReadJsonAsync();
        Assert.Equal(5m, charged.GetProperty("fee").GetDecimal());
        Assert.False(charged.GetProperty("isFree").GetBoolean());
        Assert.Contains("5.00", charged.GetProperty("message").GetString());

        var changed = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/cancel", new { reasonCode = "changed_mind", expectedFee = 4m });
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        var changedBody = await changed.ReadJsonAsync();
        Assert.Equal("cancellation_fee_changed", changedBody.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(5m, changedBody.GetProperty("error").GetProperty("details").GetProperty("fee").GetDecimal());
        Assert.Equal(TripStatus.DriverAssigned, (await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == Guid.Parse(ride.TripId)))).Status);

        var cancelled = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/cancel", new { reasonCode = "changed_mind", expectedFee = 5m })).ReadJsonAsync();
        var cancellation = cancelled.GetProperty("cancellation");
        Assert.Equal("after_accept", cancellation.GetProperty("stage").GetString());
        Assert.Equal("passenger", cancellation.GetProperty("atFault").GetString());
        Assert.Equal(5m, cancellation.GetProperty("fee").GetDecimal());
        Assert.Equal(5m, cancellation.GetProperty("feeCharged").GetDecimal());
        Assert.Equal("charged", cancellation.GetProperty("feeStatus").GetString());
        Assert.Equal("not_applicable", cancellation.GetProperty("excuseStatus").GetString());
        Assert.Equal(JsonValueKind.Null, cancellation.GetProperty("compensation").ValueKind);

        var evt = await EventAsync(ride.TripId);
        Assert.Equal(CancellationFeeMethod.Wallet, evt.FeeMethod);
        Assert.Equal(2.5m, evt.CompensationAmount);
        Assert.Equal(1, evt.PenaltyPoints);
        Assert.True(evt.CountsTowardRate);
        Assert.Equal(-5m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.Equal(2.5m, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        var movements = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.Where(t => t.ReferenceId == evt.Id).Select(t => t.Type).ToListAsync());
        Assert.Equal([TransactionType.CancellationFee, TransactionType.CancellationCompensation], movements.Order().ToList());
        var feeEntries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.Where(e => e.Account == LedgerAccounts.CancellationFees).ToListAsync());
        var feeAccount = feeEntries.Sum(e => e.Credit - e.Debit);
        Assert.True(feeAccount >= 2.5m);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == passenger.UserId && n.Type == NotificationTypes.CancellationFeeCharged)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driver.UserId && n.Type == NotificationTypes.CancellationCompensation)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driver.UserId && n.Type == NotificationTypes.TripCancelled)));

        var blocked = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area));
        Assert.Equal("outstanding_balance", await blocked.ErrorCodeAsync());

        // Appeal: approving a charged cancellation refunds the fee through F11 (cancellation_fee_waived).
        using var admin = await fixture.LoginAdminAsync();
        var waived = await (await admin.PostAsJsonAsync($"/api/v1/admin/cancellations/{evt.Id}/review", new { decision = "approve", note = "الكابتن تأخر فعلياً" })).ReadJsonAsync();
        Assert.Equal("refunded", waived.GetProperty("feeStatus").GetString());
        Assert.Equal("approved", waived.GetProperty("excuseStatus").GetString());
        Assert.Equal("none", waived.GetProperty("atFault").GetString());
        var refund = await fixture.Factory.WithDbAsync(db => db.Refunds.FirstAsync(r => r.TripId == Guid.Parse(ride.TripId)));
        Assert.Equal(RefundReasonCode.CancellationFeeWaived, refund.ReasonCode);
        Assert.Equal(RefundStatus.Succeeded, refund.Status);
        Assert.Equal(0m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/cancellations/{evt.Id}/review", new { decision = "reject", note = "x" })).StatusCode);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Card_trip_fee_is_captured_from_the_authorization_when_the_driver_is_en_route()
    {
        var area = TripFlow.Area(1);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var cardId = await PaymentFlow.AddCardAsync(passenger.Client, "tok_sandbox_visa");
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger.Client, driver.Client, PaymentFlow.CardTrip(area, cardId));
        var tripId = trip.GetProperty("id").GetString()!;
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{tripId}/en-route", null)).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));

        var preview = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel/preview", new { })).ReadJsonAsync();
        Assert.Equal("en_route", preview.GetProperty("stage").GetString());
        Assert.Equal(10m, preview.GetProperty("fee").GetDecimal());
        var cancelled = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "found_other_ride", expectedFee = 10m })).ReadJsonAsync();
        Assert.Equal(10m, cancelled.GetProperty("cancellation").GetProperty("feeCharged").GetDecimal());

        var payment = await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.TripId == Guid.Parse(tripId)));
        Assert.Equal(PaymentStatus.Captured, payment.Status);
        Assert.Equal(10m, payment.CapturedAmount);
        var evt = await EventAsync(tripId);
        Assert.Equal(CancellationFeeMethod.Card, evt.FeeMethod);
        Assert.Equal(7m, evt.CompensationAmount);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AnyAsync(j => j.Type == JournalType.CancellationFeeCard && j.ReferenceId == evt.Id)));
        Assert.Equal(0m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.Equal(7m, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Driver_cancellations_always_count_and_earn_points_after_the_free_window()
    {
        var area = TripFlow.Area(2);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var first = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        var freePreview = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{first.TripId}/cancel/preview", new { reasonCode = "pickup_too_far" })).ReadJsonAsync();
        Assert.Equal(0, freePreview.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(0m, freePreview.GetProperty("fee").GetDecimal());
        var cancelled = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{first.TripId}/cancel", new { reasonCode = "pickup_too_far" })).ReadJsonAsync();
        Assert.Equal("driver", cancelled.GetProperty("cancelledBy").GetString());
        Assert.Equal(JsonValueKind.Null, cancelled.GetProperty("cancellation").GetProperty("fee").ValueKind);
        var firstEvent = await EventAsync(first.TripId);
        Assert.Equal(AtFault.Driver, firstEvent.AtFault);
        Assert.True(firstEvent.CountsTowardRate);
        Assert.Equal(0, firstEvent.PenaltyPoints);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == passenger.UserId && n.Type == NotificationTypes.TripCancelled)));

        var second = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(61));
        var preview = await (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{second.TripId}/cancel/preview", new { reasonCode = "pickup_too_far" })).ReadJsonAsync();
        Assert.Equal(2, preview.GetProperty("penaltyPoints").GetInt32());
        Assert.Contains("+2", preview.GetProperty("message").GetString());
        var guarded = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{second.TripId}/cancel", new { reasonCode = "pickup_too_far", expectedPenaltyPoints = 1 });
        Assert.Equal("cancellation_fee_changed", await guarded.ErrorCodeAsync());
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{second.TripId}/cancel", new { reasonCode = "pickup_too_far", expectedPenaltyPoints = 2 })).EnsureSuccessStatusCode();

        var summary = await (await driver.Client.GetAsync("/api/v1/driver/reliability")).ReadJsonAsync();
        Assert.Equal("driver", summary.GetProperty("role").GetString());
        Assert.Equal(2, summary.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal(2, summary.GetProperty("cancellationsAtFault").GetInt32());
        Assert.Equal(2, summary.GetProperty("tripsAccepted").GetInt32());
        Assert.Equal(1m, summary.GetProperty("cancellationRate").GetDecimal());
        Assert.Equal("none", summary.GetProperty("level").GetString()); // the 100 % rate needs a sample of 10 trips
        Assert.Equal(1m, summary.GetProperty("effects").GetProperty("matchingFactor").GetDecimal());
        Assert.Equal("warning", summary.GetProperty("nextLevel").GetProperty("level").GetString());
        Assert.Equal(2, summary.GetProperty("recentEvents").GetArrayLength());
        Assert.Equal(2, summary.GetProperty("offersReceived").GetInt32());
    }

    [Fact]
    public async Task No_show_requires_the_wait_then_charges_the_passenger_and_compensates_the_driver()
    {
        var area = TripFlow.Area(3);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        (await ride.Driver.Client.PostAsync($"/api/v1/driver/trips/{ride.TripId}/en-route", null)).EnsureSuccessStatusCode();
        (await ride.Driver.Client.PostAsync($"/api/v1/driver/trips/{ride.TripId}/arrived", null)).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(60));
        var early = await ride.Driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{ride.TripId}/no-show", new { lat = area.Lat, lng = area.Lng });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, early.StatusCode);
        var earlyBody = await early.ReadJsonAsync();
        Assert.Equal("no_show_too_early", earlyBody.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(240, earlyBody.GetProperty("error").GetProperty("details").GetProperty("secondsRemaining").GetInt32());

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(240));
        var response = await ride.Driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{ride.TripId}/no-show", new { lat = area.Lat, lng = area.Lng });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var trip = await response.ReadJsonAsync();
        Assert.Equal("cancelled", trip.GetProperty("status").GetString());
        Assert.Equal("driver", trip.GetProperty("cancelledBy").GetString());
        Assert.Equal("passenger_no_show", trip.GetProperty("cancellationReason").GetString());
        Assert.Equal("no_show", trip.GetProperty("cancellation").GetProperty("stage").GetString());
        Assert.Equal(8m, trip.GetProperty("cancellation").GetProperty("compensation").GetDecimal());

        var evt = await EventAsync(ride.TripId);
        Assert.Equal(AtFault.Passenger, evt.AtFault);
        Assert.Equal(10m, evt.FeeCharged); // pricing rule cancellation fee 0 → min_fee 10
        Assert.Equal(4, evt.PenaltyPoints);
        Assert.Equal(-10m, await SafetyFlow.WalletBalanceAsync(fixture, ride.Passenger.UserId, WalletKind.Passenger));
        var compensation = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.SingleAsync(t => t.ReferenceId == evt.Id && t.Type == TransactionType.CancellationCompensation));
        Assert.Equal(8m, compensation.Amount);
        var types = await fixture.Factory.WithDbAsync(db => db.TripEvents.Where(e => e.TripId == Guid.Parse(ride.TripId)).Select(e => e.Type).ToListAsync());
        Assert.Contains(TripEventTypes.PassengerNoShow, types);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        var reliability = await (await ride.Passenger.Client.GetAsync("/api/v1/passenger/reliability")).ReadJsonAsync();
        Assert.Equal(1, reliability.GetProperty("noShowCount").GetInt32());
        Assert.Equal(4, reliability.GetProperty("penaltyPoints").GetInt32());
        Assert.Equal("warning", reliability.GetProperty("level").GetString());
        Assert.False(reliability.TryGetProperty("effects", out var effects) && effects.ValueKind != JsonValueKind.Null);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Passenger.UserId && n.Type == NotificationTypes.ReliabilityWarning)));
    }

    [Fact]
    public async Task Excusable_reasons_wait_for_review_approval_waives_rejection_charges_and_emergencies_open_a_case()
    {
        var area = TripFlow.Area(4);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        await TripFlow.TopupAsync(passenger.Client, 100m);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        using var admin = await fixture.LoginAdminAsync();

        var first = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{first.TripId}/en-route", null)).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        var preview = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{first.TripId}/cancel/preview", new { reasonCode = "driver_asked_to_cancel" })).ReadJsonAsync();
        Assert.True(preview.GetProperty("requiresReview").GetBoolean());
        Assert.Equal(10m, preview.GetProperty("fee").GetDecimal());
        var pending = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{first.TripId}/cancel", new { reasonCode = "driver_asked_to_cancel" })).ReadJsonAsync();
        Assert.Equal("pending_review", pending.GetProperty("cancellation").GetProperty("feeStatus").GetString());
        Assert.Equal("pending", pending.GetProperty("cancellation").GetProperty("excuseStatus").GetString());
        Assert.Equal(100m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        var firstEvent = await EventAsync(first.TripId);
        Assert.Equal(0, firstEvent.PenaltyPoints);
        Assert.False(firstEvent.CountsTowardRate);

        var queue = await (await admin.GetAsync("/api/v1/admin/cancellations/excuses")).ReadJsonAsync();
        var item = queue.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetString() == firstEvent.Id.ToString());
        Assert.Equal(2, item.GetProperty("pendingPenaltyPoints").GetInt32());
        Assert.False(item.GetProperty("slaBreached").GetBoolean());
        Assert.Equal(first.TripNumber, item.GetProperty("tripNumber").GetString());
        var approved = await (await admin.PostAsJsonAsync($"/api/v1/admin/cancellations/{firstEvent.Id}/review", new { decision = "approve", note = "تأكدنا من المحادثة" })).ReadJsonAsync();
        Assert.Equal("waived", approved.GetProperty("feeStatus").GetString());
        Assert.Equal("none", approved.GetProperty("atFault").GetString());
        Assert.Equal(100m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "cancellation.review" && a.EntityId == firstEvent.Id)));

        var second = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{second.TripId}/cancel", new { reasonCode = "driver_not_moving" })).EnsureSuccessStatusCode();
        var secondEvent = await EventAsync(second.TripId);
        fixture.Factory.Clock.Advance(TimeSpan.FromHours(49));
        using var lateAdmin = await fixture.LoginAdminAsync();
        var breached = await (await lateAdmin.GetAsync("/api/v1/admin/cancellations/excuses?status=pending")).ReadJsonAsync();
        var late = breached.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetString() == secondEvent.Id.ToString());
        Assert.True(late.GetProperty("slaBreached").GetBoolean());
        Assert.True(late.GetProperty("ageHours").GetDouble() >= 49);
        var rejected = await (await lateAdmin.PostAsJsonAsync($"/api/v1/admin/cancellations/{secondEvent.Id}/review", new { decision = "reject", note = "الكابتن كان يتحرك" })).ReadJsonAsync();
        Assert.Equal("charged", rejected.GetProperty("feeStatus").GetString());
        Assert.Equal(5m, rejected.GetProperty("feeCharged").GetDecimal());
        Assert.Equal(1, rejected.GetProperty("penaltyPoints").GetInt32());
        Assert.True(rejected.GetProperty("countsTowardRate").GetBoolean());
        Assert.Equal(95m, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.Equal(2.5m, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        Assert.Equal(HttpStatusCode.Conflict, (await lateAdmin.PostAsJsonAsync($"/api/v1/admin/cancellations/{secondEvent.Id}/review", new { decision = "approve", note = "x" })).StatusCode);

        var (freshPassenger, _) = await fixture.LoginAsync("passenger", passenger.Phone);
        var (freshDriver, _) = await fixture.LoginAsync("driver", driver.Phone);
        await TripFlow.GoOnlineAsync(freshDriver, area.Lat, area.Lng);
        var third = await SafetyFlow.NextRideAsync(fixture, area, passenger with { Client = freshPassenger }, driver with { Client = freshDriver }, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        (await freshPassenger.PostAsJsonAsync($"/api/v1/passenger/trips/{third.TripId}/cancel", new { reasonCode = "safety_concern", note = "الكابتن يقود بتهور" })).EnsureSuccessStatusCode();
        var emergency = await EventAsync(third.TripId);
        Assert.Equal(ExcuseStatus.Pending, emergency.ExcuseStatus);
        var safetyCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.SingleAsync(c => c.TripId == Guid.Parse(third.TripId)));
        Assert.Equal(SafetyCaseType.SafetyReport, safetyCase.Type);
        Assert.Equal(SafetyPriority.Medium, safetyCase.Priority);
        Assert.Equal(SafetyCaseSource.Report, safetyCase.Source);
        Assert.Equal(driver.UserId, safetyCase.SubjectUserId);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Reasons_are_validated_against_actor_stage_and_note_and_listed_by_the_catalogue()
    {
        var area = TripFlow.Area(5);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var trip = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = trip.GetProperty("id").GetString();
        foreach (var code in new[] { "no_such_reason", "vehicle_issue", "driver_too_far", "passenger_no_show" })
        {
            var invalid = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = code });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
            Assert.Equal("cancellation_reason_invalid", await invalid.ErrorCodeAsync());
        }

        var noNote = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "other" });
        Assert.Equal("required", (await noNote.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("note").GetString());
        Assert.Equal(HttpStatusCode.OK, (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "other", note = "سبب شخصي" })).StatusCode);

        using var anonymous = fixture.CreateClient(language: "en");
        var early = await (await anonymous.GetAsync("/api/v1/catalog/cancellation-reasons?actor=passenger&stage=before_accept")).ReadJsonAsync();
        var codes = early.EnumerateArray().Select(r => r.GetProperty("code").GetString()).ToList();
        Assert.Contains("changed_mind", codes);
        Assert.Contains("driver_late", codes);
        Assert.Contains("wrong_pickup", codes);
        Assert.Contains("other", codes);
        Assert.DoesNotContain("driver_too_far", codes);
        Assert.Equal("I changed my mind", early.EnumerateArray().First(r => r.GetProperty("code").GetString() == "changed_mind").GetProperty("name").GetString());
        var driverReasons = await (await anonymous.GetAsync("/api/v1/catalog/cancellation-reasons?actor=driver")).ReadJsonAsync();
        Assert.DoesNotContain(driverReasons.EnumerateArray(), r => r.GetProperty("code").GetString() == "passenger_no_show");
        Assert.True(driverReasons.EnumerateArray().Single(r => r.GetProperty("code").GetString() == "vehicle_issue").GetProperty("isExcusable").GetBoolean());
    }

    [Fact]
    public async Task Reliability_ladder_deprioritises_in_matching_then_restricts_going_online_and_lifts_after_24_hours()
    {
        var area = TripFlow.Area(6);
        using var admin = await fixture.LoginAdminAsync();
        var (risky, riskyId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "سائق متكرر الإلغاء");
        var (steady, steadyId) = await SafetyFlow.OnlineDriverAsync(fixture, (area.Lat + 0.002m, area.Lng), "سائق ملتزم");

        var deprioritized = await (await admin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}/adjust", new { role = "driver", action = "add_points", points = 8, reason = "إلغاءات متكررة" })).ReadJsonAsync();
        Assert.Equal("matching_deprioritized", deprioritized.GetProperty("level").GetString());
        Assert.Equal(0.7m, deprioritized.GetProperty("effects").GetProperty("matchingFactor").GetDecimal());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == risky.UserId && n.Type == NotificationTypes.ReliabilityWarning)));

        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var created = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = Guid.Parse(created.GetProperty("id").GetString()!);
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(JsonValueKind.Null, (await (await risky.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);
        Assert.Equal(JsonValueKind.Object, (await (await steady.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);
        var candidates = await fixture.Factory.WithDbAsync(db => db.MatchingCandidates.Where(c => db.MatchingAttempts.Any(a => a.Id == c.AttemptId && a.TripId == tripId)).ToListAsync());
        Assert.True(candidates.Single(c => c.DriverId == riskyId).Score < candidates.Single(c => c.DriverId == steadyId).Score);
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();

        var reduced = await (await admin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}/adjust", new { role = "driver", action = "add_points", points = 4, reason = "تصعيد" })).ReadJsonAsync();
        Assert.Equal("incentives_reduced", reduced.GetProperty("level").GetString());
        Assert.Equal(0.5m, reduced.GetProperty("effects").GetProperty("incentiveMultiplier").GetDecimal());
        Assert.Equal(0.6m, reduced.GetProperty("effects").GetProperty("matchingFactor").GetDecimal());

        var restricted = await (await admin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}/adjust", new { role = "driver", action = "add_points", points = 6, reason = "تقييد" })).ReadJsonAsync();
        Assert.Equal("temporarily_restricted", restricted.GetProperty("level").GetString());
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddHours(24), restricted.GetProperty("restrictedUntil").GetDateTime().ToUniversalTime());
        Assert.False(await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == riskyId).Select(d => d.IsOnline).FirstAsync()));
        var online = await risky.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = area.Lat, longitude = area.Lng });
        Assert.Equal(HttpStatusCode.Forbidden, online.StatusCode);
        var onlineBody = await online.ReadJsonAsync();
        Assert.Equal("account_restricted", onlineBody.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("temporarily_restricted", onlineBody.GetProperty("error").GetProperty("details").GetProperty("level").GetString());
        var status = await (await risky.Client.GetAsync("/api/v1/driver/status")).ReadJsonAsync();
        Assert.Equal("account_restricted", status.GetProperty("reason").GetString());
        Assert.False(status.GetProperty("canGoOnline").GetBoolean());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == risky.UserId && n.Type == NotificationTypes.ReliabilityRestricted)));
        var levelChanges = await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "reliability.level_change"));
        Assert.True(levelChanges >= 3);
        Assert.Equal(3, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "reliability.adjust" && a.EntityId == risky.UserId)));

        fixture.Factory.Clock.Advance(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1));
        Assert.True(await fixture.Factory.RunRestrictionExpiryAsync() >= 1);
        var (again, _) = await fixture.LoginAsync("driver", risky.Phone);
        Assert.Equal(HttpStatusCode.OK, (await again.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = area.Lat, longitude = area.Lng })).StatusCode);
        using var laterAdmin = await fixture.LoginAdminAsync();
        var lifted = await (await laterAdmin.GetAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}?role=driver")).ReadJsonAsync();
        Assert.Equal("incentives_reduced", lifted.GetProperty("level").GetString());
        Assert.Equal(JsonValueKind.Null, lifted.GetProperty("restrictedUntil").ValueKind);
        Assert.Equal(3, lifted.GetProperty("adjustments").GetArrayLength());

        // set_level with `until` wins over the computed level and ends by itself; clear_restriction resets the points.
        var forced = await (await laterAdmin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}/adjust",
            new { role = "driver", action = "set_level", level = "suspended", until = fixture.Factory.Clock.UtcNow.AddHours(2), reason = "تحقيق" })).ReadJsonAsync();
        Assert.Equal("suspended", forced.GetProperty("level").GetString());
        fixture.Factory.Clock.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(1));
        await fixture.Factory.RunRestrictionExpiryAsync();
        using var lastAdmin = await fixture.LoginAdminAsync();
        Assert.Equal("incentives_reduced", (await (await lastAdmin.GetAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}?role=driver")).ReadJsonAsync()).GetProperty("level").GetString());
        var cleared = await (await lastAdmin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{risky.UserId}/adjust", new { role = "driver", action = "clear_restriction", reason = "صفحة جديدة" })).ReadJsonAsync();
        Assert.Equal("none", cleared.GetProperty("level").GetString());
        Assert.Equal(0, cleared.GetProperty("penaltyPoints").GetInt32());

        var profiles = await (await lastAdmin.GetAsync("/api/v1/admin/reliability-profiles?role=driver&search=متكرر")).ReadJsonAsync();
        Assert.Equal(risky.UserId.ToString(), profiles.GetProperty("items")[0].GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Restricted_passenger_cannot_request_and_most_specific_rule_wins_in_admin_rules()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        using var admin = await fixture.LoginAdminAsync();
        (await admin.PostAsJsonAsync($"/api/v1/admin/reliability-profiles/{passenger.UserId}/adjust", new { role = "passenger", action = "add_points", points = 12, reason = "تكرار" })).EnsureSuccessStatusCode();
        var blocked = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(TripFlow.Area(7)));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("account_restricted", await blocked.ErrorCodeAsync());

        var feeBeforeAccept = await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new { name = "bad", actor = "passenger", stage = "before_accept", feeType = "fixed", feeAmount = 3 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, feeBeforeAccept.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new { name = "bad", actor = "driver", stage = "no_show", feeType = "none" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new { name = "bad", actor = "passenger", stage = "after_accept", feeType = "fixed" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new { name = "bad", actor = "passenger", stage = "after_accept", feeType = "percent", feePercent = 150 })).StatusCode);

        var specific = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new
        {
            name = "Economy in Riyadh", actor = "passenger", stage = "after_accept", rideCategoryId = SeedIds.RideCategories.Economy, zoneId = SeedIds.ZoneRiyadhDefault,
            freeWindowSeconds = 60, feeType = "fixed", feeAmount = 7, driverCompensationPercent = 50, penaltyPoints = 1,
        })).ReadJsonAsync();
        var rival = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules", new
        {
            name = "Economy in Riyadh (priority)", actor = "passenger", stage = "after_accept", rideCategoryId = SeedIds.RideCategories.Economy, zoneId = SeedIds.ZoneRiyadhDefault,
            freeWindowSeconds = 60, feeType = "percent", feePercent = 50, maxFee = 8, priority = 5, penaltyPoints = 1,
        })).ReadJsonAsync();
        var simulated = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules/simulate", new
        {
            actor = "passenger", stage = "after_accept", bookingType = "now", rideCategoryId = SeedIds.RideCategories.Economy, zoneId = SeedIds.ZoneRiyadhDefault, secondsSinceAnchor = 90, estimatedFare = 30m,
        })).ReadJsonAsync();
        Assert.Equal(rival.GetProperty("id").GetString(), simulated.GetProperty("ruleId").GetString());
        Assert.Equal(8m, simulated.GetProperty("fee").GetDecimal()); // 50 % of 30 capped by max_fee 8
        (await admin.DeleteAsync($"/api/v1/admin/cancellation-rules/{rival.GetProperty("id").GetString()}")).EnsureSuccessStatusCode();
        var specificWins = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules/simulate", new
        {
            actor = "passenger", stage = "after_accept", bookingType = "now", rideCategoryId = SeedIds.RideCategories.Economy, zoneId = SeedIds.ZoneRiyadhDefault, secondsSinceAnchor = 90, estimatedFare = 30m,
        })).ReadJsonAsync();
        Assert.Equal(7m, specificWins.GetProperty("fee").GetDecimal());
        Assert.Equal(3.5m, specificWins.GetProperty("compensation").GetDecimal());
        var generic = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-rules/simulate", new
        {
            actor = "passenger", stage = "after_accept", bookingType = "now", rideCategoryId = SeedIds.RideCategories.Comfort, secondsSinceAnchor = 90, estimatedFare = 30m,
        })).ReadJsonAsync();
        Assert.True(generic.GetProperty("isFree").GetBoolean()); // the city-wide rule still has its 120 s free window
        (await admin.DeleteAsync($"/api/v1/admin/cancellation-rules/{specific.GetProperty("id").GetString()}")).EnsureSuccessStatusCode();
        Assert.Equal(4, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action.StartsWith("cancellation_rule."))));

        var reason = await (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-reasons", new { code = "late_night", actor = "passenger", nameAr = "وقت متأخر", nameEn = "Too late", stages = new[] { "before_accept" } })).ReadJsonAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/admin/cancellation-reasons", new { code = "late_night", actor = "passenger", nameAr = "x", nameEn = "x" })).StatusCode);
        var updated = await (await admin.PutAsJsonAsync($"/api/v1/admin/cancellation-reasons/{reason.GetProperty("id").GetString()}", new { code = "late_night", actor = "passenger", nameAr = "الوقت متأخر", nameEn = "It's late", stages = (string[]?)null, isActive = true })).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("stages").ValueKind);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/cancellation-reasons/{reason.GetProperty("id").GetString()}")).StatusCode);
        var reasons = await (await admin.GetAsync("/api/v1/admin/cancellation-reasons?actor=system")).ReadJsonAsync();
        Assert.Equal(4, reasons.GetArrayLength());

        var thresholds = await (await admin.GetAsync("/api/v1/admin/reliability-thresholds?role=passenger")).ReadJsonAsync();
        Assert.Equal(3, thresholds.GetArrayLength());
        var warning = thresholds.EnumerateArray().First(t => t.GetProperty("level").GetString() == "warning");
        var changed = await (await admin.PutAsJsonAsync($"/api/v1/admin/reliability-thresholds/{warning.GetProperty("id").GetString()}", new { minPenaltyPoints = 5, minCancellationRate = 0.2m, minTripsForRate = 10, sortOrder = 1, isActive = true })).ReadJsonAsync();
        Assert.Equal(5, changed.GetProperty("minPenaltyPoints").GetInt32());
        (await admin.PutAsJsonAsync($"/api/v1/admin/reliability-thresholds/{warning.GetProperty("id").GetString()}", new { minPenaltyPoints = 4, minCancellationRate = 0.15m, minTripsForRate = 10, sortOrder = 1, isActive = true })).EnsureSuccessStatusCode();
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "reliability_threshold.update")));
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.Client.GetAsync("/api/v1/admin/cancellation-rules")).StatusCode);
    }

    private async Task<CancellationEvent> EventAsync(string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(tripId)));
}

/// <summary>KPIs of <c>GET /admin/cancellations/stats</c> on a dedicated database with hand-made data.</summary>
public class CancellationStatsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Stats_match_hand_made_cancellations()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        await TripFlow.TopupAsync(passenger.Client, 200m);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        using var admin = await fixture.LoginAdminAsync();

        // 1. cancelled while searching (nobody at fault)
        var searching = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{searching.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        // 2. passenger at fault after the free window (5 SAR)
        var paid = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{paid.TripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        // 3. driver at fault
        var byDriver = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{byDriver.TripId}/cancel", new { reasonCode = "pickup_too_far" })).EnsureSuccessStatusCode();
        // 4. completed
        await SafetyFlow.CompleteAsync(await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId));
        // 5. no-show (10 SAR)
        var noShow = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        (await driver.Client.PostAsync($"/api/v1/driver/trips/{noShow.TripId}/arrived", null)).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{noShow.TripId}/no-show", new { })).EnsureSuccessStatusCode();
        // 6. excuse approved
        var excused = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{excused.TripId}/cancel", new { reasonCode = "driver_not_moving" })).EnsureSuccessStatusCode();
        var excuseId = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.Where(e => e.TripId == Guid.Parse(excused.TripId)).Select(e => e.Id).FirstAsync());
        (await admin.PostAsJsonAsync($"/api/v1/admin/cancellations/{excuseId}/review", new { decision = "approve", note = "مقبول" })).EnsureSuccessStatusCode();

        var stats = await (await admin.GetAsync($"/api/v1/admin/cancellations/stats?from=2026-09-28&to=2026-09-28&rideCategoryId={SeedIds.RideCategories.Economy}")).ReadJsonAsync();
        Assert.Equal(5, stats.GetProperty("totalCancellations").GetInt32());
        Assert.Equal(5, stats.GetProperty("tripsAssigned").GetInt32());
        Assert.Equal(0.4m, stats.GetProperty("passengerCancellationRate").GetDecimal());
        Assert.Equal(0.2m, stats.GetProperty("driverCancellationRate").GetDecimal());
        Assert.Equal(15m, stats.GetProperty("cancellationFeeRevenue").GetDecimal());
        Assert.Equal(0.5m, stats.GetProperty("repeatCancellationRate").GetDecimal());
        Assert.Equal(0.2m, stats.GetProperty("driverReliabilityRate").GetDecimal());
        Assert.Equal(0.2m, stats.GetProperty("passengerReliabilityRate").GetDecimal());
        Assert.Equal(1m, stats.GetProperty("excuseApprovalRate").GetDecimal());
        Assert.Equal(0.5m, stats.GetProperty("noShowRate").GetDecimal());

        var none = await (await admin.GetAsync("/api/v1/admin/cancellations/stats?to=2026-09-27")).ReadJsonAsync();
        Assert.Equal(0, none.GetProperty("totalCancellations").GetInt32());

        var events = await (await admin.GetAsync($"/api/v1/admin/cancellations?search={paid.TripNumber}")).ReadJsonAsync();
        var row = Assert.Single(events.GetProperty("items").EnumerateArray());
        Assert.Equal("passenger", row.GetProperty("atFault").GetString());
        Assert.Equal(5m, row.GetProperty("feeCharged").GetDecimal());
        Assert.Equal("سارة أحمد", row.GetProperty("userName").GetString());
        var noShows = await (await admin.GetAsync("/api/v1/admin/cancellations?stage=no_show")).ReadJsonAsync();
        Assert.Equal(1, noShows.GetProperty("total").GetInt32());

        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{paid.TripId}")).ReadJsonAsync();
        Assert.Equal(2.5m, detail.GetProperty("cancellation").GetProperty("compensation").GetDecimal());
        Assert.Equal(1, detail.GetProperty("cancellation").GetProperty("penaltyPoints").GetInt32());
        Assert.True(detail.GetProperty("plannedRoute").GetArrayLength() >= 2);
        var adminCancel = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        var forced = await (await admin.PostAsJsonAsync($"/api/v1/admin/trips/{adminCancel.TripId}/cancel", new { reason = "بلاغ", atFault = "passenger", chargeFee = true })).ReadJsonAsync();
        Assert.Equal("admin_cancelled", forced.GetProperty("cancellationReason").GetString());
        Assert.Equal(5m, forced.GetProperty("cancellation").GetProperty("feeCharged").GetDecimal());
        Assert.Equal("admin", forced.GetProperty("cancellation").GetProperty("actor").GetString());
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }
}
