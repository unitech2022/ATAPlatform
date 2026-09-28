using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Payments;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>Refunds of 20 SAR or more need a second admin in this fixture (so a ~30 SAR trip exercises the four-eyes rule).</summary>
public sealed class PaymentsFixture() : ApiFixture(new Dictionary<string, string?> { ["Payments:RefundAutoApproveLimit"] = "20" });

public class PaymentsTests(PaymentsFixture fixture) : IClassFixture<PaymentsFixture>
{
    [Fact]
    public async Task Three_ds_trip_waits_in_requested_then_searches_after_the_challenge_or_is_cancelled_on_expiry()
    {
        var area = TripFlow.Area(5);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var pendingCard = await (await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_3ds" })).ReadJsonAsync();
        var cardId = pendingCard.GetProperty("paymentMethod").GetProperty("id").GetString()!;
        using var browser = PaymentFlow.NoRedirectClient(fixture);
        await browser.PostAsync($"/api/v1/payments/sandbox/challenge/{cardId}", new FormUrlEncodedContent(new Dictionary<string, string> { ["outcome"] = "approve" }));

        var first = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", PaymentFlow.CardTrip(area, cardId))).ReadJsonAsync();
        Assert.Equal("requested", first.GetProperty("status").GetString());
        var action = first.GetProperty("payment").GetProperty("action");
        Assert.Equal("redirect", action.GetProperty("type").GetString());
        var paymentId = first.GetProperty("payment").GetProperty("id").GetString();
        var approved = await browser.PostAsync(new Uri(action.GetProperty("url").GetString()!).PathAndQuery, new FormUrlEncodedContent(new Dictionary<string, string> { ["outcome"] = "approve" }));
        Assert.Equal(HttpStatusCode.Redirect, approved.StatusCode);
        Assert.Contains($"paymentId={paymentId}", approved.Headers.Location!.ToString());
        Assert.Contains("status=authorized", approved.Headers.Location!.ToString());
        var searching = await (await passenger.GetAsync($"/api/v1/passenger/trips/{first.GetProperty("id").GetString()}")).ReadJsonAsync();
        Assert.Equal("searching", searching.GetProperty("status").GetString());
        (await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{first.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        Assert.Equal(PaymentStatus.Voided, (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == Guid.Parse(paymentId!)))).Status);

        var second = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", PaymentFlow.CardTrip(area, cardId))).ReadJsonAsync();
        var secondId = Guid.Parse(second.GetProperty("id").GetString()!);
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(TripStatus.Requested, (await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == secondId))).Status);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(301));
        Assert.True(await fixture.Factory.WithServiceAsync<PaymentJobs, int>(j => j.ExpireActionsAsync(CancellationToken.None)) >= 1);
        var expired = await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == secondId));
        Assert.Equal(TripStatus.Cancelled, expired.Status);
        Assert.Equal("payment_failed", expired.CancellationReason);
        Assert.Equal(PaymentStatus.Failed, (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.TripId == secondId))).Status);
    }

    [Fact]
    public async Task Declined_card_rejects_the_trip_request_without_creating_a_trip()
    {
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_visa");
        await fixture.Factory.WithDbAsync(async db =>
        {
            var card = await db.PaymentMethods.FirstAsync(m => m.Id == Guid.Parse(cardId));
            card.GatewayToken = "tok_sandbox_declined";
            return await db.SaveChangesAsync();
        });
        var response = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", PaymentFlow.CardTrip(TripFlow.Area(6), cardId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("payment_failed", error.GetProperty("code").GetString());
        Assert.Equal("card_declined", error.GetProperty("details").GetProperty("failureCode").GetString());
        Assert.Equal(JsonValueKind.Null, (await (await passenger.GetAsync("/api/v1/passenger/trips/active")).ReadJsonAsync()).ValueKind);
    }

    [Fact]
    public async Task Cards_are_saved_from_sandbox_tokens_with_3ds_duplicate_and_decline_handling()
    {
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var config = await (await passenger.GetAsync("/api/v1/payments/config")).ReadJsonAsync();
        Assert.Equal("sandbox", config.GetProperty("provider").GetString());
        Assert.Contains(config.GetProperty("sandboxTokens").EnumerateArray(), t => t.GetString() == "tok_sandbox_3ds");

        var created = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_visa", setDefault = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var card = await created.ReadJsonAsync();
        Assert.Equal("active", card.GetProperty("status").GetString());
        Assert.Equal("visa", card.GetProperty("brand").GetString());
        Assert.Equal("4242", card.GetProperty("last4").GetString());
        Assert.True(card.GetProperty("isDefault").GetBoolean());
        Assert.False(card.GetProperty("isExpired").GetBoolean());
        var stored = await fixture.Factory.WithDbAsync(db => db.PaymentMethods.FirstAsync(m => m.Id == Guid.Parse(card.GetProperty("id").GetString()!)));
        Assert.Equal("tok_sandbox_visa", stored.GatewayToken);
        Assert.Equal(4, stored.Last4.Length);

        var duplicate = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_visa" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var declined = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_declined" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, declined.StatusCode);
        Assert.Equal("payment_failed", await declined.ErrorCodeAsync());

        var pending = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_3ds", returnUrl = "ata://payments/return" });
        Assert.Equal(HttpStatusCode.Accepted, pending.StatusCode);
        var pendingBody = await pending.ReadJsonAsync();
        Assert.Equal("pending_verification", pendingBody.GetProperty("paymentMethod").GetProperty("status").GetString());
        var actionUrl = new Uri(pendingBody.GetProperty("action").GetProperty("url").GetString()!);
        using var browser = PaymentFlow.NoRedirectClient(fixture);
        Assert.Contains("form", await browser.GetStringAsync(actionUrl.PathAndQuery));
        var approved = await browser.PostAsync(actionUrl.PathAndQuery, new FormUrlEncodedContent(new Dictionary<string, string> { ["outcome"] = "approve" }));
        Assert.Equal(HttpStatusCode.Redirect, approved.StatusCode);
        Assert.StartsWith("ata://payments/return", approved.Headers.Location!.ToString());
        Assert.Contains("status=active", approved.Headers.Location!.ToString());

        var list = await (await passenger.GetAsync("/api/v1/passenger/payment-methods")).ReadJsonAsync();
        Assert.Equal(2, list.GetArrayLength());
        var wallet = await (await passenger.GetAsync("/api/v1/wallet")).ReadJsonAsync();
        Assert.Contains(wallet.GetProperty("paymentMethods").EnumerateArray(), m => m.GetProperty("type").GetString() == "card" && m.GetProperty("last4").GetString() == "4242");

        var threeDsId = pendingBody.GetProperty("paymentMethod").GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.DeleteAsync($"/api/v1/passenger/payment-methods/{threeDsId}")).StatusCode);
        Assert.Equal(1, (await (await passenger.GetAsync("/api/v1/passenger/payment-methods")).ReadJsonAsync()).GetArrayLength());
    }

    [Fact]
    public async Task Card_trip_is_authorized_with_buffer_and_captured_on_completion()
    {
        var area = TripFlow.Area(0);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_mada");
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن البطاقة");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        var created = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", PaymentFlow.CardTrip(area, cardId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trip = await created.ReadJsonAsync();
        var tripId = trip.GetProperty("id").GetString()!;
        Assert.Equal("searching", trip.GetProperty("status").GetString());
        var payment = trip.GetProperty("payment");
        Assert.Equal("authorized", payment.GetProperty("status").GetString());
        Assert.Equal("mada", payment.GetProperty("brand").GetString());
        var estimated = trip.GetProperty("estimatedFare").GetDecimal();
        Assert.Equal(Math.Ceiling(estimated * 1.3m), payment.GetProperty("authorizedAmount").GetDecimal());

        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal("card", offer.GetProperty("paymentMethod").GetString());
        (await driver.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId, pin);
        var completed = await (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync();
        Assert.Equal("card", completed.GetProperty("paymentMethod").GetString());
        Assert.Equal(JsonValueKind.Null, completed.GetProperty("collectCashAmount").ValueKind);
        var fare = completed.GetProperty("finalFare").GetDecimal();
        Assert.Equal("captured", completed.GetProperty("payment").GetProperty("status").GetString());
        Assert.Equal(fare, completed.GetProperty("payment").GetProperty("capturedAmount").GetDecimal());

        var journal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.SingleAsync(j => j.ReferenceId == Guid.Parse(tripId) && j.Type == JournalType.TripCardCapture));
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.Where(e => e.JournalId == journal.Id).ToListAsync());
        Assert.Contains(entries, e => e.Account == LedgerAccounts.GatewayClearing && e.Debit == fare);
        Assert.Contains(entries, e => e.Account == LedgerAccounts.TripRevenue && e.Credit == fare);
        var earning = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.SingleAsync(t => t.ReferenceId == Guid.Parse(tripId)));
        Assert.Equal(TransactionType.TripEarning, earning.Type);
        var driverBalance = await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.UserId == PaymentFlow.UserId(driverAuth) && w.Kind == WalletKind.Driver).Select(w => w.Balance).FirstAsync());
        Assert.Equal(earning.Amount, driverBalance);

        var receipt = await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        Assert.Equal(fare, receipt.GetProperty("total").GetDecimal());
        Assert.Equal(fare, receipt.GetProperty("lines").EnumerateArray().Sum(l => l.GetProperty("amount").GetDecimal()));
        Assert.Equal(decimal.Round(fare * 15m / 115m, 2), receipt.GetProperty("vatIncluded").GetDecimal());
        Assert.Equal("card", receipt.GetProperty("payment").GetProperty("method").GetString());
        Assert.Equal("4201", receipt.GetProperty("payment").GetProperty("last4").GetString());
        Assert.Contains(receipt.GetProperty("lines").EnumerateArray(), l => l.GetProperty("code").GetString() == "base_fare");
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        _ = passengerAuth;
    }

    [Fact]
    public async Task Declined_capture_falls_back_to_cash_and_notifies_the_passenger()
    {
        var area = TripFlow.Area(1);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_capture_fail");
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن النقد");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, PaymentFlow.CardTrip(area, cardId));
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId, pin);
        var completed = await (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync();
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        Assert.Equal("cash", completed.GetProperty("paymentMethod").GetString());
        var fare = completed.GetProperty("finalFare").GetDecimal();
        Assert.Equal(fare, completed.GetProperty("collectCashAmount").GetDecimal());
        Assert.Contains(completed.GetProperty("events").EnumerateArray(), e => e.GetProperty("type").GetString() == "payment_fallback_cash");

        var passengerView = await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, passengerView.GetProperty("collectCashAmount").ValueKind);
        var fallback = await fixture.Factory.WithDbAsync(db => db.TripEvents.SingleAsync(e => e.TripId == Guid.Parse(tripId) && e.Type == TripEventTypes.PaymentFallbackCash));
        Assert.Contains("card_capture_failed", fallback.Data);
        var payment = await fixture.Factory.WithDbAsync(db => db.Payments.SingleAsync(p => p.TripId == Guid.Parse(tripId)));
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        var passengerUserId = PaymentFlow.UserId(passengerAuth);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == passengerUserId && n.Type == NotificationTypes.PaymentFailed)));

        var driverWallet = await fixture.Factory.WithDbAsync(db => db.Wallets.FirstAsync(w => w.UserId == PaymentFlow.UserId(driverAuth) && w.Kind == WalletKind.Driver));
        var driverEarnings = (await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == Guid.Parse(tripId)))).DriverEarnings!.Value;
        Assert.Equal(driverEarnings - fare, driverWallet.Balance);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Unknown_capture_result_retries_then_becomes_passenger_debt_that_blocks_new_requests()
    {
        var area = TripFlow.Area(2);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_timeout");
        var (driver, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن المهلة");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, PaymentFlow.CardTrip(area, cardId));
        var tripId = Guid.Parse(trip.GetProperty("id").GetString()!);
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId.ToString(), pin);
        var completed = await (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync();
        Assert.Equal("card", completed.GetProperty("paymentMethod").GetString());
        var fare = completed.GetProperty("finalFare").GetDecimal();
        var pending = await fixture.Factory.WithDbAsync(db => db.Payments.SingleAsync(p => p.TripId == tripId));
        Assert.Equal(PaymentStatus.Authorized, pending.Status);
        Assert.Contains("capturePending", pending.Metadata);

        for (var i = 0; i < 6 && (await fixture.Factory.WithDbAsync(db => db.Payments.SingleAsync(p => p.TripId == tripId))).Status == PaymentStatus.Authorized; i++)
        {
            await fixture.Factory.WithServiceAsync<PaymentJobs, int>(j => j.RetryCapturesAsync(CancellationToken.None));
        }

        Assert.Equal(PaymentStatus.Failed, (await fixture.Factory.WithDbAsync(db => db.Payments.SingleAsync(p => p.TripId == tripId))).Status);
        var wallet = await (await passenger.GetAsync("/api/v1/wallet")).ReadJsonAsync();
        Assert.Equal(-fare, wallet.GetProperty("balance").GetDecimal());

        var blocked = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);
        var error = (await blocked.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("outstanding_balance", error.GetProperty("code").GetString());
        Assert.Equal(fare, error.GetProperty("details").GetProperty("amount").GetDecimal());

        await TripFlow.TopupAsync(passenger, fare + 10m);
        Assert.Equal(HttpStatusCode.Created, (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).StatusCode);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == PaymentFlow.UserId(passengerAuth) && n.Type == NotificationTypes.PaymentFailed)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Card_topup_credits_once_is_idempotent_and_3ds_completes_through_a_signed_webhook()
    {
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_mastercard");

        var first = await PaymentFlow.PostWithKeyAsync(passenger, "/api/v1/wallet/topups", new { amount = 100, method = "card", paymentMethodId = cardId }, "card-topup-1");
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var body = await first.ReadJsonAsync();
        Assert.Equal("captured", body.GetProperty("status").GetString());
        Assert.Equal(100m, body.GetProperty("balance").GetDecimal());
        var replay = await (await PaymentFlow.PostWithKeyAsync(passenger, "/api/v1/wallet/topups", new { amount = 100, method = "card", paymentMethodId = cardId }, "card-topup-1")).ReadJsonAsync();
        Assert.Equal(body.GetProperty("transactionId").GetString(), replay.GetProperty("transactionId").GetString());
        Assert.Equal(body.GetProperty("paymentId").GetString(), replay.GetProperty("paymentId").GetString());
        var payment = await (await passenger.GetAsync($"/api/v1/payments/{body.GetProperty("paymentId").GetString()}")).ReadJsonAsync();
        Assert.Equal("topup", payment.GetProperty("purpose").GetString());
        Assert.Equal("5454", payment.GetProperty("card").GetProperty("last4").GetString());

        var missingCard = await PaymentFlow.PostWithKeyAsync(passenger, "/api/v1/wallet/topups", new { amount = 100, method = "card" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingCard.StatusCode);

        // 3-D Secure top-up: 202 with an action, credited only when the (signed) webhook reports the capture — once, even when repeated.
        var pendingCard = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token = "tok_sandbox_3ds" });
        var pendingCardId = (await pendingCard.ReadJsonAsync()).GetProperty("paymentMethod").GetProperty("id").GetString();
        using (var browser = PaymentFlow.NoRedirectClient(fixture))
        {
            await browser.PostAsync($"/api/v1/payments/sandbox/challenge/{pendingCardId}", new FormUrlEncodedContent(new Dictionary<string, string> { ["outcome"] = "approve" }));
        }

        var accepted = await PaymentFlow.PostWithKeyAsync(passenger, "/api/v1/wallet/topups", new { amount = 50, method = "card", paymentMethodId = pendingCardId });
        Assert.Equal(HttpStatusCode.Accepted, accepted.StatusCode);
        var acceptedBody = await accepted.ReadJsonAsync();
        Assert.Equal("initiated", acceptedBody.GetProperty("status").GetString());
        Assert.Equal("redirect", acceptedBody.GetProperty("action").GetProperty("type").GetString());
        var paymentId = Guid.Parse(acceptedBody.GetProperty("paymentId").GetString()!);
        var gatewayId = (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == paymentId))).GatewayPaymentId!;

        var webhook = new { id = $"evt_{Guid.NewGuid():N}", type = "payment.captured", data = new { id = gatewayId, status = "captured", amount = 50.00m } };
        Assert.Equal(HttpStatusCode.OK, (await PaymentFlow.PostWebhookAsync(fixture, webhook)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PaymentFlow.PostWebhookAsync(fixture, webhook)).StatusCode);
        var retried = new { id = $"evt_{Guid.NewGuid():N}", type = "payment.captured", data = new { id = gatewayId, status = "captured", amount = 50.00m } };
        Assert.Equal(HttpStatusCode.OK, (await PaymentFlow.PostWebhookAsync(fixture, retried)).StatusCode);

        var balance = (await (await passenger.GetAsync("/api/v1/wallet")).ReadJsonAsync()).GetProperty("balance").GetDecimal();
        Assert.Equal(150m, balance);
        var events = await fixture.Factory.WithDbAsync(db => db.PaymentWebhookEvents.Where(e => e.GatewayPaymentId == gatewayId).ToListAsync());
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.ProcessingStatus == WebhookProcessingStatus.Processed);
        Assert.Contains(events, e => e.ProcessingStatus == WebhookProcessingStatus.Ignored);
        var walletId = Guid.Parse((await (await passenger.GetAsync("/api/v1/wallet")).ReadJsonAsync()).GetProperty("id").GetString()!);
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.WalletTransactions.CountAsync(t => t.WalletId == walletId && t.Type == TransactionType.Topup)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Webhook_with_bad_signature_is_rejected_and_stored_and_stale_states_are_ignored()
    {
        var raw = JsonSerializer.Serialize(new { id = "evt_forged", type = "payment.captured", data = new { id = "sbx_unknown", status = "captured" } });
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhooks/sandbox") { Content = new StringContent(raw) };
        request.Headers.Add("X-Sandbox-Signature", "deadbeef");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("webhook_signature_invalid", await response.ErrorCodeAsync());
        var stored = await fixture.Factory.WithDbAsync(db => db.PaymentWebhookEvents.SingleAsync(e => e.GatewayPaymentId == "sbx_unknown"));
        Assert.False(stored.SignatureValid);

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/v1/payments/webhooks/unknown", new StringContent("{}"))).StatusCode);

        var (passenger, _) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_visa");
        var topup = await (await PaymentFlow.PostWithKeyAsync(passenger, "/api/v1/wallet/topups", new { amount = 20, method = "card", paymentMethodId = cardId })).ReadJsonAsync();
        var paymentId = Guid.Parse(topup.GetProperty("paymentId").GetString()!);
        var gatewayId = (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == paymentId))).GatewayPaymentId!;
        var stale = new { id = "evt_stale_1", type = "payment.authorized", data = new { id = gatewayId, status = "authorized" } };
        Assert.Equal(HttpStatusCode.OK, (await PaymentFlow.PostWebhookAsync(fixture, stale)).StatusCode);
        var evt = await fixture.Factory.WithDbAsync(db => db.PaymentWebhookEvents.SingleAsync(e => e.EventId == "evt_stale_1"));
        Assert.Equal(WebhookProcessingStatus.Ignored, evt.ProcessingStatus);
        Assert.Equal(PaymentStatus.Captured, (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == paymentId))).Status);
    }

    [Fact]
    public async Task Refunds_partial_then_full_with_four_eyes_and_wallet_refunds_for_cash_trips()
    {
        var area = TripFlow.Area(3);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_visa");
        var (driver, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن الاسترداد");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, PaymentFlow.CardTrip(area, cardId));
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId, pin);
        var fare = (await (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).ReadJsonAsync()).GetProperty("finalFare").GetDecimal();
        Assert.True(fare > 20m);
        var paymentId = (await fixture.Factory.WithDbAsync(db => db.Payments.SingleAsync(p => p.TripId == Guid.Parse(tripId) && p.Status == PaymentStatus.Captured))).Id;

        using var admin = await fixture.LoginAdminAsync();
        var partial = await admin.PostAsJsonAsync($"/api/v1/admin/payments/{paymentId}/refunds", new { amount = 5, reasonCode = "service_issue", reason = "تأخر الكابتن" });
        Assert.Equal(HttpStatusCode.Created, partial.StatusCode);
        var partialBody = await partial.ReadJsonAsync();
        Assert.Equal("succeeded", partialBody.GetProperty("status").GetString());
        Assert.Equal("partial", partialBody.GetProperty("type").GetString());
        Assert.StartsWith("R-20260928-", partialBody.GetProperty("refundNumber").GetString());
        Assert.Equal(PaymentStatus.PartiallyRefunded, (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == paymentId))).Status);

        var tooMuch = await admin.PostAsJsonAsync($"/api/v1/admin/payments/{paymentId}/refunds", new { amount = fare, reasonCode = "fare_dispute", reason = "x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMuch.StatusCode);
        var tooMuchError = (await tooMuch.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("refund_exceeds_amount", tooMuchError.GetProperty("code").GetString());
        Assert.Equal(fare - 5m, tooMuchError.GetProperty("details").GetProperty("refundable").GetDecimal());

        var rest = await (await admin.PostAsJsonAsync($"/api/v1/admin/trips/{tripId}/refunds", new { amount = fare - 5m, reasonCode = "fare_dispute", reason = "شكوى الأجرة" })).ReadJsonAsync();
        Assert.Equal("pending_approval", rest.GetProperty("status").GetString());
        var restId = rest.GetProperty("id").GetString();
        var sameAdmin = await admin.PostAsync($"/api/v1/admin/refunds/{restId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, sameAdmin.StatusCode);
        Assert.Equal("four_eyes_required", await sameAdmin.ErrorCodeAsync());

        using var second = await PaymentFlow.SecondAdminAsync(fixture, "finance2");
        var approved = await (await second.PostAsync($"/api/v1/admin/refunds/{restId}/approve", null)).ReadJsonAsync();
        Assert.Equal("succeeded", approved.GetProperty("status").GetString());
        Assert.Equal("Admin finance2", approved.GetProperty("approvedByName").GetString());
        Assert.Equal("ATA Admin", approved.GetProperty("requestedByName").GetString());
        Assert.Equal(PaymentStatus.Refunded, (await fixture.Factory.WithDbAsync(db => db.Payments.FirstAsync(p => p.Id == paymentId))).Status);
        var refundJournals = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.CountAsync(j => j.Type == JournalType.RefundCard));
        Assert.True(refundJournals >= 2);

        var detail = await (await admin.GetAsync($"/api/v1/admin/payments/{paymentId}")).ReadJsonAsync();
        Assert.Equal(2, detail.GetProperty("refunds").GetArrayLength());
        Assert.Contains(detail.GetProperty("ledger").EnumerateArray(), l => l.GetProperty("type").GetString() == "refund_card");
        var list = await (await admin.GetAsync("/api/v1/admin/payments?purpose=trip&status=refunded")).ReadJsonAsync();
        Assert.Contains(list.GetProperty("items").EnumerateArray(), p => p.GetProperty("id").GetString() == paymentId.ToString());
        var receipt = await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        Assert.Equal(0m, receipt.GetProperty("netPaid").GetDecimal());

        // Cash trip → refund to the wallet (refund movement: refunds → passenger_wallet).
        var cashTrip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, TripFlow.Request(area));
        var cashTripId = cashTrip.GetProperty("id").GetString()!;
        var cashPin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{cashTripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, cashTripId, cashPin);
        (await driver.PostAsJsonAsync($"/api/v1/driver/trips/{cashTripId}/complete", new { })).EnsureSuccessStatusCode();
        var walletRefund = await (await admin.PostAsJsonAsync($"/api/v1/admin/trips/{cashTripId}/refunds", new { amount = 7.5m, reasonCode = "goodwill", reason = "تعويض" })).ReadJsonAsync();
        Assert.Equal("wallet", walletRefund.GetProperty("destination").GetString());
        Assert.Equal("succeeded", walletRefund.GetProperty("status").GetString());
        var refundTx = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.SingleAsync(t => t.Type == TransactionType.Refund && t.ReferenceId == Guid.Parse(walletRefund.GetProperty("id").GetString()!)));
        Assert.Equal(7.5m, refundTx.Amount);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == PaymentFlow.UserId(passengerAuth) && n.Type == NotificationTypes.PaymentRefunded)));
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "refund").Select(a => a.Action).ToListAsync());
        Assert.Contains("refund.create", audits);
        Assert.Contains("refund.approve", audits);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Payouts_request_approve_pay_reject_and_batch_keep_the_ledger_balanced()
    {
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن السحب");
        var walletId = Guid.Parse((await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync()).GetProperty("id").GetString()!);
        using var admin = await fixture.LoginAdminAsync();
        var adjusted = await admin.PostAsJsonAsync($"/api/v1/admin/wallets/{walletId}/adjustments", new { direction = "credit", amount = 400, reason = "رصيد افتتاحي" });
        Assert.Equal(HttpStatusCode.Created, adjusted.StatusCode);

        var noIban = await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 150 });
        Assert.Equal("iban_missing", await noIban.ErrorCodeAsync());
        await fixture.Factory.WithDbAsync(async db =>
        {
            var d = await db.Drivers.FirstAsync(x => x.Id == driverId);
            d.Iban = "SA0380000000608010167519";
            return await db.SaveChangesAsync();
        });
        var small = await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 50 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, small.StatusCode);
        Assert.Equal("payout_below_minimum", await small.ErrorCodeAsync());
        var tooMuch = await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 1000 });
        Assert.Equal("insufficient_balance", await tooMuch.ErrorCodeAsync());

        var requested = await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 150 }, "payout-1");
        Assert.Equal(HttpStatusCode.Created, requested.StatusCode);
        var payout = await requested.ReadJsonAsync();
        Assert.Equal("requested", payout.GetProperty("status").GetString());
        Assert.Equal("SA03 **** **** 7519", payout.GetProperty("ibanMasked").GetString());
        Assert.StartsWith("PO-20260928-", payout.GetProperty("payoutNumber").GetString());
        Assert.Equal(payout.GetProperty("id").GetString(), (await (await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 150 }, "payout-1")).ReadJsonAsync()).GetProperty("id").GetString());
        var second = await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 100 });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("payout_pending_exists", await second.ErrorCodeAsync());
        var summary = await (await driver.GetAsync("/api/v1/driver/payouts/summary")).ReadJsonAsync();
        Assert.Equal(250m, summary.GetProperty("balance").GetDecimal());
        Assert.Equal("payout_pending_exists", summary.GetProperty("reason").GetString());

        var payoutId = payout.GetProperty("id").GetString();
        var listed = await (await admin.GetAsync($"/api/v1/admin/payouts?driverId={driverId}&status=requested")).ReadJsonAsync();
        Assert.Equal(1, listed.GetProperty("total").GetInt32());
        Assert.Equal("كابتن السحب", listed.GetProperty("items")[0].GetProperty("driverName").GetString());
        Assert.Equal("approved", (await (await admin.PostAsync($"/api/v1/admin/payouts/{payoutId}/approve", null)).ReadJsonAsync()).GetProperty("status").GetString());
        var paid = await (await admin.PostAsJsonAsync($"/api/v1/admin/payouts/{payoutId}/mark-paid", new { bankReference = "BANK-001" })).ReadJsonAsync();
        Assert.Equal("paid", paid.GetProperty("status").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AnyAsync(j => j.Type == JournalType.PayoutPaid && j.ReferenceId == Guid.Parse(payoutId!))));

        var toReject = await (await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 100 })).ReadJsonAsync();
        var rejected = await (await admin.PostAsJsonAsync($"/api/v1/admin/payouts/{toReject.GetProperty("id").GetString()}/reject", new { reason = "آيبان غير مطابق" })).ReadJsonAsync();
        Assert.Equal("rejected", rejected.GetProperty("status").GetString());
        var toCancel = await (await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 100 })).ReadJsonAsync();
        Assert.Equal("cancelled", (await (await driver.PostAsync($"/api/v1/driver/payouts/{toCancel.GetProperty("id").GetString()}/cancel", null)).ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(250m, (await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync()).GetProperty("balance").GetDecimal());

        var batched = await (await PaymentFlow.PostWithKeyAsync(driver, "/api/v1/driver/payouts", new { amount = 120 })).ReadJsonAsync();
        await admin.PostAsync($"/api/v1/admin/payouts/{batched.GetProperty("id").GetString()}/approve", null);
        var batchResponse = await admin.PostAsJsonAsync("/api/v1/admin/payout-batches", new { allApproved = true });
        Assert.Equal(HttpStatusCode.Created, batchResponse.StatusCode);
        var batch = await batchResponse.ReadJsonAsync();
        Assert.Equal(1, batch.GetProperty("payoutsCount").GetInt32());
        var csv = await admin.GetAsync($"/api/v1/admin/payout-batches/{batch.GetProperty("id").GetString()}/export?format=csv");
        Assert.Equal("text/csv", csv.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".csv", csv.Content.Headers.ContentDisposition!.FileName!.Trim('"'));
        var text = System.Text.Encoding.UTF8.GetString(await csv.Content.ReadAsByteArrayAsync()).TrimStart('﻿');
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("payout_number,beneficiary_name,iban,amount,currency,reference", lines[0]);
        Assert.Contains("SA0380000000608010167519,120.00,SAR", lines[1]);
        var paidBatch = await (await admin.PostAsJsonAsync($"/api/v1/admin/payout-batches/{batch.GetProperty("id").GetString()}/mark-paid", new { bankReference = "BATCH-9" })).ReadJsonAsync();
        Assert.Equal("paid", paidBatch.GetProperty("status").GetString());
        Assert.Equal("paid", paidBatch.GetProperty("payouts")[0].GetProperty("status").GetString());

        Assert.Equal(130m, (await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync()).GetProperty("balance").GetDecimal());
        var pendingNet = await fixture.Factory.WithDbAsync(async db =>
        {
            var payoutIds = await db.Payouts.Where(p => p.DriverId == driverId).Select(p => (Guid?)p.Id).ToListAsync();
            var txIds = await db.WalletTransactions.Where(t => payoutIds.Contains(t.ReferenceId)).Select(t => (Guid?)t.Id).ToListAsync();
            var journalIds = await db.LedgerJournals.Where(j => payoutIds.Contains(j.ReferenceId)).Select(j => (Guid?)j.Id).ToListAsync();
            var rows = await db.LedgerEntries.Where(e => e.Account == LedgerAccounts.PayoutsPending && (txIds.Contains(e.TransactionId) || journalIds.Contains(e.JournalId))).ToListAsync();
            return rows.Sum(e => e.Credit - e.Debit);
        });
        Assert.Equal(0m, pendingNet);
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Select(a => a.Action).ToListAsync());
        Assert.Contains("payout.mark_paid", audits);
        Assert.Contains("payout_batch.export", audits);
        Assert.Contains("wallet.adjust", audits);
        var walletsResponse = await admin.GetAsync($"/api/v1/admin/wallets?kind=driver&search={Uri.EscapeDataString(driverAuth.GetProperty("user").GetProperty("phoneNumber").GetString()!)}");
        var wallets = await walletsResponse.ReadJsonAsync();
        Assert.True(walletsResponse.IsSuccessStatusCode, wallets.ToString());
        Assert.Equal(1, wallets.GetProperty("total").GetInt32());
        Assert.Equal("كابتن السحب", wallets.GetProperty("items")[0].GetProperty("name").GetString());
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Cash_debt_over_the_limit_blocks_going_online_and_matching()
    {
        var area = TripFlow.Area(4);
        var (driver, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن المديونية");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);
        var walletId = (await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync()).GetProperty("id").GetString();
        using var admin = await fixture.LoginAdminAsync();
        (await admin.PostAsJsonAsync($"/api/v1/admin/wallets/{walletId}/adjustments", new { direction = "debit", amount = 600, reason = "عمولات نقدية" })).EnsureSuccessStatusCode();

        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Created, (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).StatusCode);
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(JsonValueKind.Null, (await (await driver.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);

        (await driver.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        var online = await driver.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = true, latitude = area.Lat, longitude = area.Lng });
        Assert.Equal(HttpStatusCode.Forbidden, online.StatusCode);
        var error = (await online.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("cash_debt_limit_exceeded", error.GetProperty("code").GetString());
        Assert.Equal(600m, error.GetProperty("details").GetProperty("cashDebt").GetDecimal());
        Assert.Equal(500m, error.GetProperty("details").GetProperty("limit").GetDecimal());
        var wallet = await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync();
        Assert.Equal(600m, wallet.GetProperty("cashDebt").GetDecimal());
        Assert.Equal(500m, wallet.GetProperty("cashDebtLimit").GetDecimal());

        var negative = await (await admin.GetAsync("/api/v1/admin/wallets?negativeOnly=true&kind=driver")).ReadJsonAsync();
        Assert.Contains(negative.GetProperty("items").EnumerateArray(), w => w.GetProperty("id").GetString() == walletId);
        var balances = await (await admin.GetAsync("/api/v1/admin/ledger/balances")).ReadJsonAsync();
        Assert.Contains(balances.EnumerateArray(), b => b.GetProperty("account").GetString() == "driver_wallets");
        Assert.Contains(balances.EnumerateArray(), b => b.GetProperty("account").GetString() == LedgerAccounts.Adjustments);
    }
}
