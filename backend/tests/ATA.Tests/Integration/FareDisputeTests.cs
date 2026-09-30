using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Support;
using ATA.Domain.Wallet;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class FareDisputeTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    private static object Dispute(string reason = "overcharged", decimal? requested = 10m) => new { reason, requestedRefundAmount = requested };

    private async Task<decimal> FareAsync(string tripId) =>
        await fixture.Factory.WithDbAsync(db => db.Trips.Where(t => t.Id == Guid.Parse(tripId)).Select(t => t.FinalFare!.Value).FirstAsync());

    private async Task<(SafetyFlow.Ride Ride, string TicketId, string DisputeId, decimal Fare)> OpenDisputeAsync(int area, decimal? requested = 10m)
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(area));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "تم احتساب مبلغ أعلى", "الأجرة أعلى من المتوقع", Dispute(requested: requested));
        var disputeId = await fixture.Factory.WithDbAsync(db => db.FareDisputes.Where(d => d.TicketId == Guid.Parse(Str(ticket, "id"))).Select(d => d.Id).FirstAsync());
        return (ride, Str(ticket, "id"), disputeId.ToString(), await FareAsync(ride.TripId));
    }

    private Task<decimal> WalletAsync(Guid userId) => SafetyFlow.WalletBalanceAsync(fixture, userId, WalletKind.Passenger);

    [Fact]
    public async Task A_dispute_opens_with_a_payment_issue_on_a_completed_trip_and_only_one_per_trip()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(0));
        var fare = await FareAsync(ride.TripId);
        Assert.True(fare > 20m);

        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "تم احتساب مبلغ أعلى", "الأجرة أعلى من المتوقع", Dispute());
        Assert.Equal("high", Str(ticket, "priority"));
        var dispute = ticket.GetProperty("dispute");
        Assert.Equal("overcharged", Str(dispute, "reason"));
        Assert.Equal(fare, dispute.GetProperty("chargedAmount").GetDecimal());
        Assert.Equal(10m, dispute.GetProperty("requestedRefundAmount").GetDecimal());
        Assert.Equal("open", Str(dispute, "status"));
        Assert.Equal(JsonValueKind.Null, dispute.GetProperty("resolution").ValueKind);
        Assert.Equal(JsonValueKind.Null, dispute.GetProperty("approvedRefundAmount").ValueKind);
        var row = await fixture.Factory.WithDbAsync(db => db.FareDisputes.AsNoTracking().SingleAsync(d => d.TripId == Guid.Parse(ride.TripId)));
        Assert.Equal(Guid.Parse(Str(ticket, "id")), row.TicketId);
        Assert.Equal(ride.Passenger.UserId, row.RequesterUserId);

        // One dispute per trip (409 dispute_exists, nothing else is created); a payment issue without a dispute is fine.
        var ticketsBefore = await fixture.Factory.WithDbAsync(db => db.SupportTickets.CountAsync(t => t.RequesterUserId == ride.Passenger.UserId));
        var duplicate = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new
        {
            type = "payment_issue", tripId = ride.TripId, subject = "مرة ثانية", message = "اعتراض ثانٍ", dispute = Dispute("route_longer"),
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("dispute_exists", await duplicate.ErrorCodeAsync());
        Assert.Equal(ticketsBefore, await fixture.Factory.WithDbAsync(db => db.SupportTickets.CountAsync(t => t.RequesterUserId == ride.Passenger.UserId)));
        Assert.Equal("open", Str(await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "سؤال عن الدفع", "كيف دُفع المبلغ؟"), "status"));

        // The dispute belongs to a payment issue of the passenger.
        var wrongType = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "trip_issue", tripId = ride.TripId, subject = "x", message = "y", dispute = Dispute() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongType.StatusCode);
        var driverDispute = await ride.Driver.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "payment_issue", tripId = ride.TripId, subject = "x", message = "y", dispute = Dispute() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, driverDispute.StatusCode);
        Assert.Equal("passenger_only", (await driverDispute.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("dispute").GetString());
    }

    [Fact]
    public async Task Dispute_fields_the_trip_state_and_the_window_are_validated()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(1));
        var fare = await FareAsync(ride.TripId);

        async Task<JsonElement> RejectedAsync(object dispute, string tripId)
        {
            var response = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "payment_issue", tripId, subject = "x", message = "y", dispute });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            return (await response.ReadJsonAsync()).GetProperty("error");
        }

        Assert.Equal("required", (await RejectedAsync(new { requestedRefundAmount = 5 }, ride.TripId)).GetProperty("details").GetProperty("dispute.reason").GetString());
        Assert.Equal("exceeds_charged", (await RejectedAsync(Dispute(requested: fare + 1), ride.TripId)).GetProperty("details").GetProperty("dispute.requestedRefundAmount").GetString());
        Assert.Equal("must be positive", (await RejectedAsync(Dispute(requested: 0m), ride.TripId)).GetProperty("details").GetProperty("dispute.requestedRefundAmount").GetString());
        await RejectedAsync(Dispute(requested: 1.234m), ride.TripId);
        Assert.Equal(0, await fixture.Factory.WithDbAsync(db => db.FareDisputes.CountAsync(d => d.TripId == Guid.Parse(ride.TripId))));
        // The requested amount is optional.
        Assert.Equal("open", Str((await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "بلا مبلغ", "تفاصيل", Dispute(requested: null))).GetProperty("dispute"), "status"));

        // A trip that is not finished cannot be disputed.
        var running = await SafetyFlow.NextRideAsync(fixture, TripFlow.Area(1), ride.Passenger, ride.Driver, ride.DriverId);
        var notDisputable = await RejectedAsync(Dispute(), running.TripId);
        Assert.Equal("not_disputable", notDisputable.GetProperty("details").GetProperty("tripId").GetString());

        // The window: 14 days after the trip completed (inclusive), else 422 dispute_window_closed.
        var late = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(2));
        await SetCompletedAtAsync(late.TripId, fixture.Factory.Clock.UtcNow.AddDays(-15));
        var closed = await late.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "payment_issue", tripId = late.TripId, subject = "x", message = "y", dispute = Dispute() });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, closed.StatusCode);
        var closedBody = (await closed.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("dispute_window_closed", closedBody.GetProperty("code").GetString());
        Assert.Equal(14, closedBody.GetProperty("details").GetProperty("windowDays").GetInt32());
        await SetCompletedAtAsync(late.TripId, fixture.Factory.Clock.UtcNow.AddDays(-14));
        Assert.Equal("open", Str((await SupportFlow.CreateTicketAsync(late.Passenger.Client, "payment_issue", late.TripId, "آخر يوم", "تفاصيل", Dispute())).GetProperty("dispute"), "status"));
    }

    private Task SetCompletedAtAsync(string tripId, DateTime completedAt) =>
        fixture.Factory.WithDbAsync(async db =>
        {
            var trip = await db.Trips.FirstAsync(t => t.Id == Guid.Parse(tripId));
            trip.CompletedAt = completedAt;
            await db.SaveChangesAsync();
            return true;
        });

    [Fact]
    public async Task A_partial_refund_below_the_four_eyes_limit_is_executed_at_once_to_the_wallet_of_a_cash_trip()
    {
        var (ride, ticketId, disputeId, fare) = await OpenDisputeAsync(3);
        var admin = await fixture.LoginAdminAsync();
        var adminId = await SafetyFlow.AdminUserIdAsync(fixture);
        var walletBefore = await WalletAsync(ride.Passenger.UserId);

        var response = await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "refund_partial", amount = 10m, note = "تم تأكيد زيادة في الأجرة" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resolved = await response.ReadJsonAsync();
        Assert.Equal("partially_approved", Str(resolved, "status"));
        Assert.Equal("refund_partial", Str(resolved, "resolution"));
        Assert.Equal(10m, resolved.GetProperty("approvedRefundAmount").GetDecimal());
        Assert.Equal(fare, resolved.GetProperty("chargedAmount").GetDecimal());
        Assert.Equal(adminId.ToString(), Str(resolved, "resolvedBy"));
        Assert.Equal("تم تأكيد زيادة في الأجرة", Str(resolved, "resolutionNote"));
        var refund = resolved.GetProperty("refund");
        Assert.Equal("succeeded", Str(refund, "status"));
        Assert.Equal("wallet", Str(refund, "destination"));
        Assert.Equal(10m, refund.GetProperty("amount").GetDecimal());
        Assert.Matches(@"^R-\d{8}-\d{5}$", Str(refund, "refundNumber"));
        Assert.Equal(Str(refund, "id"), Str(resolved, "refundId"));

        var stored = await fixture.Factory.WithDbAsync(db => db.Refunds.AsNoTracking().SingleAsync(r => r.Id == Guid.Parse(Str(refund, "id"))));
        Assert.Equal(RefundReasonCode.FareDispute, stored.ReasonCode);
        Assert.Equal(Guid.Parse(disputeId), stored.DisputeId);
        Assert.Equal(Guid.Parse(ride.TripId), stored.TripId);
        Assert.Equal(ride.Passenger.UserId, stored.UserId);
        Assert.Equal(RefundType.Partial, stored.Type);
        Assert.Equal(adminId, stored.RequestedBy);
        Assert.Equal(walletBefore + 10m, await WalletAsync(ride.Passenger.UserId));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // The ticket: a system line, the support.status notice, the audit entry and the passenger's view of the dispute.
        var detail = await SupportFlow.UserTicketAsync(ride.Passenger.Client, ticketId);
        var line = detail.GetProperty("messages").EnumerateArray().Last();
        Assert.Equal("system", Str(line, "authorRole"));
        Assert.Contains("10.00", Str(line, "body"));
        var userDispute = detail.GetProperty("dispute");
        Assert.Equal("partially_approved", Str(userDispute, "status"));
        Assert.Equal("refund_partial", Str(userDispute, "resolution"));
        Assert.Equal(10m, userDispute.GetProperty("approvedRefundAmount").GetDecimal());
        var notice = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().FirstAsync(n => n.UserId == ride.Passenger.UserId && n.Type == NotificationTypes.SupportStatus));
        Assert.Contains("استرداد جزئي", notice.BodyAr);
        Assert.Equal($"ata://support/tickets/{ticketId}", JsonDocument.Parse(notice.Data!).RootElement.GetProperty("deepLink").GetString());
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "support_dispute.resolve" && a.EntityId == Guid.Parse(disputeId))));
        var adminDetail = await SupportFlow.AdminTicketAsync(admin, ticketId);
        Assert.Equal("partially_approved", Str(adminDetail.GetProperty("dispute"), "status"));
        Assert.Equal("succeeded", Str(adminDetail.GetProperty("dispute").GetProperty("refund"), "status"));

        // Resolved once.
        var again = await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "no_refund", note = "مرة ثانية" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task A_full_refund_above_the_limit_waits_for_a_second_admin_under_the_four_eyes_rule()
    {
        var (ride, ticketId, disputeId, fare) = await OpenDisputeAsync(4, requested: null);
        var admin = await fixture.LoginAdminAsync();
        var walletBefore = await WalletAsync(ride.Passenger.UserId);

        var resolved = await (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "refund_full", note = "الأجرة غير صحيحة" })).ReadJsonAsync();
        Assert.Equal("approved", Str(resolved, "status"));
        Assert.Equal(fare, resolved.GetProperty("approvedRefundAmount").GetDecimal());
        var refund = resolved.GetProperty("refund");
        Assert.Equal("pending_approval", Str(refund, "status"));
        Assert.Equal(Guid.Parse(disputeId), Guid.Parse(Str(refund, "disputeId")));
        var refundId = Str(refund, "id");
        Assert.Equal(walletBefore, await WalletAsync(ride.Passenger.UserId));

        var sameAdmin = await admin.PostAsync($"{Api}/admin/refunds/{refundId}/approve", null);
        Assert.Equal(HttpStatusCode.Conflict, sameAdmin.StatusCode);
        Assert.Equal("four_eyes_required", await sameAdmin.ErrorCodeAsync());
        Assert.Equal(walletBefore, await WalletAsync(ride.Passenger.UserId));

        var second = await PaymentFlow.SecondAdminAsync(fixture, "dispute-approver");
        var approved = await (await second.PostAsync($"{Api}/admin/refunds/{refundId}/approve", null)).ReadJsonAsync();
        Assert.Equal("succeeded", Str(approved, "status"));
        Assert.Equal(walletBefore + fare, await WalletAsync(ride.Passenger.UserId));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Passenger.UserId && n.Type == NotificationTypes.PaymentRefunded)));

        // The passenger was told the refund follows a final review; the dispute list and the ticket detail show the refund state.
        var line = (await SupportFlow.UserTicketAsync(ride.Passenger.Client, ticketId)).GetProperty("messages").EnumerateArray().Last();
        Assert.Contains("المراجعة النهائية", Str(line, "body"));
        var listed = (await (await admin.GetAsync($"{Api}/admin/support/disputes?status=approved&pageSize=100")).ReadJsonAsync()).GetProperty("items").EnumerateArray().First(d => Str(d, "id") == disputeId);
        Assert.Equal("succeeded", Str(listed.GetProperty("refund"), "status"));
        Assert.Equal(Str(listed.GetProperty("refund"), "refundNumber"), Str(approved, "refundNumber"));
    }

    [Fact]
    public async Task Rejecting_a_dispute_creates_no_refund_and_resolution_input_is_validated()
    {
        var (ride, ticketId, disputeId, fare) = await OpenDisputeAsync(5);
        var admin = await fixture.LoginAdminAsync();
        var refundsBefore = await fixture.Factory.WithDbAsync(db => db.Refunds.CountAsync());

        async Task<JsonElement> InvalidAsync(object body)
        {
            var response = await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", body);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            return (await response.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        }

        Assert.Equal("required", (await InvalidAsync(new { resolution = "no_refund" })).GetProperty("note").GetString());
        Assert.Equal("required", (await InvalidAsync(new { note = "x" })).GetProperty("resolution").GetString());
        Assert.Equal("required for refund_partial", (await InvalidAsync(new { resolution = "refund_partial", note = "x" })).GetProperty("amount").GetString());
        Assert.Equal("exceeds_charged", (await InvalidAsync(new { resolution = "refund_partial", amount = fare + 0.5m, note = "x" })).GetProperty("amount").GetString());
        Assert.Equal("required for refund_partial", (await InvalidAsync(new { resolution = "refund_partial", amount = 0, note = "x" })).GetProperty("amount").GetString());
        Assert.Equal("at most 2 decimal places", (await InvalidAsync(new { resolution = "refund_partial", amount = 1.234m, note = "x" })).GetProperty("amount").GetString());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "bogus", note = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{Guid.NewGuid()}/resolve", new { resolution = "no_refund", note = "x" })).StatusCode);
        Assert.Equal(refundsBefore, await fixture.Factory.WithDbAsync(db => db.Refunds.CountAsync()));

        // A refund that the trip cannot bear (already refunded elsewhere) fails as refund_exceeds_amount and leaves the dispute open.
        var walletBefore = await WalletAsync(ride.Passenger.UserId);
        await fixture.Factory.WithDbAsync(async db =>
        {
            db.Refunds.Add(new Refund
            {
                RefundNumber = "R-TEST-00001", TripId = Guid.Parse(ride.TripId), UserId = ride.Passenger.UserId, Amount = fare - 1m, Type = RefundType.Partial, Destination = RefundDestination.Wallet,
                ReasonCode = RefundReasonCode.Goodwill, Reason = "earlier goodwill", Status = RefundStatus.Succeeded, RequestedBy = Guid.NewGuid(),
            });
            await db.SaveChangesAsync();
            return true;
        });
        var exceeds = await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "refund_full", note = "x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, exceeds.StatusCode);
        Assert.Equal("refund_exceeds_amount", await exceeds.ErrorCodeAsync());
        Assert.Equal(DisputeStatus.Open, (await fixture.Factory.WithDbAsync(db => db.FareDisputes.AsNoTracking().SingleAsync(d => d.Id == Guid.Parse(disputeId)))).Status);

        var rejected = await (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "no_refund", note = "الأجرة صحيحة" })).ReadJsonAsync();
        Assert.Equal("rejected", Str(rejected, "status"));
        Assert.Equal("no_refund", Str(rejected, "resolution"));
        Assert.Equal(JsonValueKind.Null, rejected.GetProperty("approvedRefundAmount").ValueKind);
        Assert.Equal(JsonValueKind.Null, rejected.GetProperty("refund").ValueKind);
        Assert.Equal(JsonValueKind.Null, rejected.GetProperty("refundId").ValueKind);
        Assert.Equal(refundsBefore + 1, await fixture.Factory.WithDbAsync(db => db.Refunds.CountAsync()));
        Assert.Equal(walletBefore, await WalletAsync(ride.Passenger.UserId));
        var line = (await SupportFlow.UserTicketAsync(ride.Passenger.Client, ticketId)).GetProperty("messages").EnumerateArray().Last();
        Assert.Equal("system", Str(line, "authorRole"));
        Assert.Contains("لم تتم الموافقة", Str(line, "body"));
        var userDispute = (await SupportFlow.UserTicketAsync(ride.Passenger.Client, ticketId)).GetProperty("dispute");
        Assert.Equal("rejected", Str(userDispute, "status"));
        Assert.Equal("no_refund", Str(userDispute, "resolution"));
    }

    [Fact]
    public async Task A_fee_charged_on_a_cancelled_trip_can_be_disputed_and_is_refunded_to_the_wallet()
    {
        var area = TripFlow.Area(6);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        var cancelled = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/passenger/trips/{ride.TripId}/cancel", new { reasonCode = "changed_mind", expectedFee = 5m });
        cancelled.EnsureSuccessStatusCode();
        Assert.Equal(-5m, await WalletAsync(ride.Passenger.UserId));

        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "رسوم إلغاء", "لم يصل الكابتن", Dispute("cancellation_fee", 5m));
        Assert.Equal(5m, ticket.GetProperty("dispute").GetProperty("chargedAmount").GetDecimal());
        Assert.Equal("cancellation_fee", Str(ticket.GetProperty("dispute"), "reason"));
        var disputeId = (await fixture.Factory.WithDbAsync(db => db.FareDisputes.Where(d => d.TripId == Guid.Parse(ride.TripId)).Select(d => d.Id).FirstAsync())).ToString();

        var admin = await fixture.LoginAdminAsync();
        var resolved = await (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "refund_full", note = "إلغاء بسبب تأخر الكابتن" })).ReadJsonAsync();
        Assert.Equal("approved", Str(resolved, "status"));
        Assert.Equal(5m, resolved.GetProperty("approvedRefundAmount").GetDecimal());
        Assert.Equal("succeeded", Str(resolved.GetProperty("refund"), "status"));
        Assert.Equal(0m, await WalletAsync(ride.Passenger.UserId));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // A trip cancelled for free has nothing to dispute.
        var free = await SafetyFlow.NextRideAsync(fixture, area, ride.Passenger, ride.Driver, ride.DriverId);
        (await ride.Passenger.Client.PostAsJsonAsync($"{Api}/passenger/trips/{free.TripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var nothing = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "payment_issue", tripId = free.TripId, subject = "x", message = "y", dispute = Dispute("cancellation_fee", null) });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, nothing.StatusCode);
    }

    [Fact]
    public async Task A_partial_refund_of_a_card_trip_goes_back_to_the_original_card()
    {
        var area = TripFlow.Area(7);
        var (passenger, passengerAuth) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(passenger, "tok_sandbox_visa");
        var (driver, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن البطاقة");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, PaymentFlow.CardTrip(area, cardId));
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.GetAsync($"{Api}/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId, pin);
        (await driver.PostAsJsonAsync($"{Api}/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        var payment = await fixture.Factory.WithDbAsync(db => db.Payments.AsNoTracking().SingleAsync(p => p.TripId == Guid.Parse(tripId) && p.Status == PaymentStatus.Captured));

        var ticket = await SupportFlow.CreateTicketAsync(passenger, "payment_issue", tripId, "خصم أعلى", "خُصم مبلغ أعلى من بطاقتي", Dispute());
        Assert.Equal(payment.CapturedAmount, ticket.GetProperty("dispute").GetProperty("chargedAmount").GetDecimal());
        var disputeId = (await fixture.Factory.WithDbAsync(db => db.FareDisputes.Where(d => d.TripId == Guid.Parse(tripId)).Select(d => d.Id).FirstAsync())).ToString();
        var admin = await fixture.LoginAdminAsync();
        var resolved = await (await admin.PostAsJsonAsync($"{Api}/admin/support/disputes/{disputeId}/resolve", new { resolution = "refund_partial", amount = 10, note = "زيادة" })).ReadJsonAsync();
        var refund = resolved.GetProperty("refund");
        Assert.Equal("original_method", Str(refund, "destination"));
        Assert.Equal(payment.Id.ToString(), Str(refund, "paymentId"));
        Assert.Equal("succeeded", Str(refund, "status"));
        var after = await fixture.Factory.WithDbAsync(db => db.Payments.AsNoTracking().SingleAsync(p => p.Id == payment.Id));
        Assert.Equal(10m, after.RefundedAmount);
        Assert.Equal(PaymentStatus.PartiallyRefunded, after.Status);
        Assert.Equal(0m, await SafetyFlow.WalletBalanceAsync(fixture, PaymentFlow.UserId(passengerAuth), WalletKind.Passenger));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Dispute_resolution_needs_support_disputes_and_the_queue_lists_open_disputes_first()
    {
        var first = await OpenDisputeAsync(8);
        var second = await OpenDisputeAsync(9);
        var viewer = await SupportFlow.AdminWithAsync(fixture, "dispute-viewer", "support.view");
        var manager = await SupportFlow.AdminWithAsync(fixture, "dispute-manager", "support.manage", "support.view");
        var resolver = await SupportFlow.AdminWithAsync(fixture, "dispute-resolver", "support.disputes");

        var body = new { resolution = "no_refund", note = "مراجعة" };
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"{Api}/admin/support/disputes/{first.DisputeId}/resolve", body)).StatusCode);
        var denied = await manager.PostAsJsonAsync($"{Api}/admin/support/disputes/{first.DisputeId}/resolve", body);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("forbidden", await denied.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await resolver.GetAsync($"{Api}/admin/support/disputes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/support/disputes")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await first.Ride.Passenger.Client.GetAsync($"{Api}/admin/support/disputes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await resolver.PostAsJsonAsync($"{Api}/admin/support/disputes/{first.DisputeId}/resolve", body)).StatusCode);

        // Open disputes first (oldest first), resolved ones after; filters by status and trip.
        var list = (await (await viewer.GetAsync($"{Api}/admin/support/disputes?pageSize=100")).ReadJsonAsync()).GetProperty("items").EnumerateArray().ToList();
        var ids = list.Select(d => Str(d, "id")).ToList();
        Assert.True(ids.IndexOf(second.DisputeId) < ids.IndexOf(first.DisputeId));
        var firstResolved = list.FindIndex(d => Str(d, "status") is not ("open" or "under_review"));
        Assert.True(firstResolved > 0);
        Assert.All(list.Skip(firstResolved), d => Assert.DoesNotContain(Str(d, "status"), new[] { "open", "under_review" }));
        var open = await (await viewer.GetAsync($"{Api}/admin/support/disputes?status=open&pageSize=100")).ReadJsonAsync();
        Assert.Contains(open.GetProperty("items").EnumerateArray(), d => Str(d, "id") == second.DisputeId);
        Assert.DoesNotContain(open.GetProperty("items").EnumerateArray(), d => Str(d, "id") == first.DisputeId);
        var byTrip = await (await viewer.GetAsync($"{Api}/admin/support/disputes?tripId={first.Ride.TripId}")).ReadJsonAsync();
        Assert.Equal(1, byTrip.GetProperty("total").GetInt32());
        var row = byTrip.GetProperty("items")[0];
        Assert.Equal(first.Ride.TripNumber, Str(row, "tripNumber"));
        Assert.False(string.IsNullOrEmpty(Str(row, "ticketNumber")));
        Assert.Equal("rejected", Str(row, "status"));
        Assert.False(string.IsNullOrEmpty(Str(row, "requesterName")));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await viewer.GetAsync($"{Api}/admin/support/disputes?status=bogus")).StatusCode);

        // First admin action on a dispute's ticket marks the dispute as under review.
        var agent = await fixture.LoginAdminAsync();
        (await SupportFlow.AdminPostAsync(agent, second.TicketId, "messages", new { body = "نراجع اعتراضك", isInternal = false })).EnsureSuccessStatusCode();
        Assert.Equal(DisputeStatus.UnderReview, (await fixture.Factory.WithDbAsync(db => db.FareDisputes.AsNoTracking().SingleAsync(d => d.Id == Guid.Parse(second.DisputeId)))).Status);
        Assert.Equal("under_review", Str((await SupportFlow.UserTicketAsync(second.Ride.Passenger.Client, second.TicketId)).GetProperty("dispute"), "status"));
    }
}
