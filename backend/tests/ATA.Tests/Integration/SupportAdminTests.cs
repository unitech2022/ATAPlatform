using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Support;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SupportAdminTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    [Fact]
    public async Task The_ticket_detail_carries_the_requester_trip_receipt_dispute_and_linked_cases()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(2));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "payment_issue", ride.TripId, "اعتراض", "الأجرة أعلى", new { reason = "waiting_charged", requestedRefundAmount = 4m });
        var id = Str(ticket, "id");
        var admin = await fixture.LoginAdminAsync();
        var detail = await SupportFlow.AdminTicketAsync(admin, id);

        var requester = detail.GetProperty("requester");
        Assert.Equal(ride.Passenger.UserId.ToString(), Str(requester, "userId"));
        Assert.Equal("سارة أحمد", Str(requester, "fullName"));
        Assert.Equal(ride.Passenger.Phone, Str(requester, "phoneNumber"));
        Assert.Equal("passenger", Str(requester, "role"));
        Assert.Equal("ar", Str(requester, "language"));
        Assert.Equal(JsonValueKind.Null, requester.GetProperty("driverId").ValueKind);

        var trip = detail.GetProperty("trip");
        Assert.Equal(ride.TripNumber, Str(trip, "tripNumber"));
        Assert.Equal("completed", Str(trip, "status"));
        Assert.Equal("المنزل", Str(trip, "pickupName"));
        Assert.Equal("العمل", Str(trip, "dropoffName"));
        Assert.Equal("cash", Str(trip, "paymentMethod"));
        Assert.Equal("محمد العتيبي", Str(trip, "driverName"));
        var fare = trip.GetProperty("finalFare").GetDecimal();
        Assert.True(fare > 0);
        Assert.Equal(fare, trip.GetProperty("receipt").GetProperty("total").GetDecimal());
        Assert.True(trip.GetProperty("estimatedFare").GetDecimal() > 0);

        Assert.Equal("waiting_charged", Str(detail.GetProperty("dispute"), "reason"));
        Assert.Equal(fare, detail.GetProperty("dispute").GetProperty("chargedAmount").GetDecimal());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("dispute").GetProperty("refund").ValueKind);
        Assert.Equal("app", Str(detail, "channel"));
        Assert.Equal("high", Str(detail, "priority"));
        Assert.Contains(Str(detail, "slaState"), new[] { "ok", "due_soon" });
        Assert.Equal(0, detail.GetProperty("slaPausedSeconds").GetInt32());
        Assert.Equal(0, detail.GetProperty("unreadByUser").GetInt32());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("firstResponseAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("firstResponseDueAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, detail.GetProperty("updatedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("linked").GetProperty("safetyCase").ValueKind);

        // A driver's ticket exposes the driver id for the profile link.
        var driverTicket = await SupportFlow.CreateTicketAsync(ride.Driver.Client, "trip_issue", ride.TripId, "مشكلة", "تفاصيل");
        var driverDetail = await SupportFlow.AdminTicketAsync(admin, Str(driverTicket, "id"));
        Assert.Equal("driver", Str(driverDetail.GetProperty("requester"), "role"));
        Assert.Equal(ride.DriverId.ToString(), Str(driverDetail.GetProperty("requester"), "driverId"));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Api}/admin/support/tickets/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Canned_responses_are_created_updated_filtered_and_deleted_with_audit()
    {
        var admin = await fixture.LoginAdminAsync();
        var seeded = await (await admin.GetAsync($"{Api}/admin/canned-responses")).ReadJsonAsync();
        var codes = seeded.EnumerateArray().Select(c => Str(c, "code")).ToList();
        foreach (var code in new[] { "greeting", "need_more_info", "refund_approved", "refund_rejected", "lost_item_contacted", "closing" })
        {
            Assert.Contains(code, codes);
        }

        var refundApproved = seeded.EnumerateArray().First(c => Str(c, "code") == "refund_approved");
        Assert.Equal("payment_issue", Str(refundApproved, "ticketType"));
        Assert.Contains("{userName}", Str(refundApproved, "bodyAr"));
        Assert.Contains("{tripNumber}", Str(refundApproved, "bodyEn"));

        var created = await admin.PostAsJsonAsync($"{Api}/admin/canned-responses", new { code = "crud_test", title = "اختبار", bodyAr = "نص {userName}", bodyEn = "Text {userName}", ticketType = "lost_item", isActive = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdBody = await created.ReadJsonAsync();
        var id = Str(createdBody, "id");
        Assert.Equal("lost_item", Str(createdBody, "ticketType"));
        Assert.True(createdBody.GetProperty("isActive").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"{Api}/admin/canned-responses", new { code = "crud_test", title = "x", bodyAr = "x", bodyEn = "x" })).StatusCode);
        foreach (var bad in new object[]
                 {
                     new { code = "Bad Code", title = "x", bodyAr = "x", bodyEn = "x" },
                     new { code = new string('a', 41), title = "x", bodyAr = "x", bodyEn = "x" },
                     new { code = "no_title", title = "", bodyAr = "x", bodyEn = "x" },
                     new { code = "no_body", title = "x", bodyAr = "", bodyEn = "x" },
                     new { code = "bad_type", title = "x", bodyAr = "x", bodyEn = "x", ticketType = "nonsense" },
                 })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"{Api}/admin/canned-responses", bad)).StatusCode);
        }

        var updated = await admin.PutAsJsonAsync($"{Api}/admin/canned-responses/{id}", new { code = "crud_test", title = "محدّث", bodyAr = "جديد", bodyEn = "New", ticketType = (string?)null, isActive = false });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await updated.ReadJsonAsync();
        Assert.Equal("محدّث", Str(updatedBody, "title"));
        Assert.Equal(JsonValueKind.Null, updatedBody.GetProperty("ticketType").ValueKind);
        Assert.False(updatedBody.GetProperty("isActive").GetBoolean());
        var clash = await admin.PutAsJsonAsync($"{Api}/admin/canned-responses/{id}", new { code = "greeting", title = "x", bodyAr = "x", bodyEn = "x" });
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{Api}/admin/canned-responses/{Guid.NewGuid()}", new { code = "x_y", title = "x", bodyAr = "x", bodyEn = "x" })).StatusCode);

        // Filters: by type (generic responses are offered for every type) and by active flag.
        var forLostItems = (await (await admin.GetAsync($"{Api}/admin/canned-responses?ticketType=lost_item")).ReadJsonAsync()).EnumerateArray().Select(c => Str(c, "code")).ToList();
        Assert.Contains("lost_item_contacted", forLostItems);
        Assert.Contains("greeting", forLostItems);
        Assert.DoesNotContain("refund_approved", forLostItems);
        var inactive = (await (await admin.GetAsync($"{Api}/admin/canned-responses?active=false")).ReadJsonAsync()).EnumerateArray().Select(c => Str(c, "code")).ToList();
        Assert.Equal(["crud_test"], inactive);

        // An inactive response cannot be inserted into a ticket.
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var ticket = await SupportFlow.CreateTicketAsync(passenger.Client);
        var unusable = await SupportFlow.AdminPostAsync(admin, Str(ticket, "id"), "messages", new { isInternal = false, cannedResponseCode = "crud_test" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unusable.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Api}/admin/canned-responses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"{Api}/admin/canned-responses/{id}")).StatusCode);
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "canned_response").Select(a => a.Action).ToListAsync());
        foreach (var action in new[] { "canned_response.create", "canned_response.update", "canned_response.delete" })
        {
            Assert.Contains(action, actions);
        }
    }

    [Fact]
    public async Task Sla_policies_are_listed_replaced_validated_and_used_for_new_tickets()
    {
        var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var seeded = (await (await admin.GetAsync($"{Api}/admin/support/sla-policies")).ReadJsonAsync()).EnumerateArray().ToList();
        Assert.Equal(["urgent", "high", "normal", "low"], seeded.Select(p => Str(p, "priority")).ToArray());
        Assert.Equal([(15, 240), (60, 1440), (240, 2880), (1440, 4320)], seeded.Select(p => (p.GetProperty("firstResponseMinutes").GetInt32(), p.GetProperty("resolutionMinutes").GetInt32())).ToArray());

        async Task<HttpResponseMessage> PutAsync(object body) => await admin.PutAsJsonAsync($"{Api}/admin/support/sla-policies", body);

        // The body is an array (also accepted: { policies: [...] } or one policy).
        var replaced = await PutAsync(new[] { new { priority = "normal", firstResponseMinutes = 30, resolutionMinutes = 600 } });
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        var afterReplace = (await replaced.ReadJsonAsync()).EnumerateArray().ToList();
        Assert.Equal(4, afterReplace.Count);
        var normal = afterReplace.First(p => Str(p, "priority") == "normal");
        Assert.Equal((30, 600), (normal.GetProperty("firstResponseMinutes").GetInt32(), normal.GetProperty("resolutionMinutes").GetInt32()));
        Assert.Equal(15, afterReplace.First(p => Str(p, "priority") == "urgent").GetProperty("firstResponseMinutes").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(new { policies = new[] { new { priority = "low", firstResponseMinutes = 2000, resolutionMinutes = 5000 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(new { priority = "high", firstResponseMinutes = 45, resolutionMinutes = 1000 })).StatusCode);

        // A new ticket follows the updated policy.
        var ticket = await SupportFlow.CreateTicketAsync(passenger.Client, "other");
        var row = await SupportFlow.TicketAsync(fixture, Str(ticket, "id"));
        Assert.Equal(row.CreatedAt.AddMinutes(30), row.FirstResponseDueAt);
        Assert.Equal(row.CreatedAt.AddMinutes(600), row.ResolutionDueAt);

        // Validation: 1–525600 minutes, resolution not below first response, one policy per priority.
        foreach (var bad in new object[]
                 {
                     new[] { new { priority = "normal", firstResponseMinutes = 0, resolutionMinutes = 60 } },
                     new[] { new { priority = "normal", firstResponseMinutes = 10, resolutionMinutes = 525601 } },
                     new[] { new { priority = "normal", firstResponseMinutes = 100, resolutionMinutes = 50 } },
                     new[] { new { priority = "normal", firstResponseMinutes = 10, resolutionMinutes = 60 }, new { priority = "normal", firstResponseMinutes = 20, resolutionMinutes = 70 } },
                     new[] { new { priority = "bogus", firstResponseMinutes = 10, resolutionMinutes = 60 } },
                     Array.Empty<object>(),
                 })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PutAsync(bad)).StatusCode);
        }

        Assert.Equal(1, (await fixture.Factory.WithDbAsync(db => db.SupportSlaPolicies.CountAsync(p => p.Priority == SupportPriority.Normal))));

        // Restore the seed values for the other tests and keep the audit trail.
        var restored = await PutAsync(new object[]
        {
            new { priority = "urgent", firstResponseMinutes = 15, resolutionMinutes = 240 }, new { priority = "high", firstResponseMinutes = 60, resolutionMinutes = 1440 },
            new { priority = "normal", firstResponseMinutes = 240, resolutionMinutes = 2880 }, new { priority = "low", firstResponseMinutes = 1440, resolutionMinutes = 4320 },
        });
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(4, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "support_sla.update")));
    }

    [Fact]
    public async Task Support_endpoints_check_support_view_support_manage_support_disputes_and_help_manage()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var ticket = await SupportFlow.CreateTicketAsync(passenger.Client);
        var id = Str(ticket, "id");
        var viewer = await SupportFlow.AdminWithAsync(fixture, "perm-viewer", "support.view");
        var manager = await SupportFlow.AdminWithAsync(fixture, "perm-manager", "support.manage");
        var helper = await SupportFlow.AdminWithAsync(fixture, "perm-helper", "help.manage");
        var agentId = await SafetyFlow.AdminUserIdAsync(fixture);

        // Viewer: read everything, change nothing.
        foreach (var path in new[] { "summary", "stats", "tickets", $"tickets/{id}", "disputes" })
        {
            Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{Api}/admin/support/{path}")).StatusCode);
        }

        var writes = new (string Path, object Body)[]
        {
            ($"tickets/{id}/messages", new { body = "x", isInternal = false }), ($"tickets/{id}/assign", new { userId = agentId }), ($"tickets/{id}/status", new { status = "in_progress" }),
            ($"tickets/{id}/priority", new { priority = "high" }), ($"tickets/{id}/type", new { type = "account" }),
            ("tickets", new { requesterUserId = passenger.UserId, type = "other", subject = "x", message = "y" }),
        };
        foreach (var (path, body) in writes)
        {
            var denied = await viewer.PostAsJsonAsync($"{Api}/admin/support/{path}", body);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            Assert.Equal("forbidden", await denied.ErrorCodeAsync());
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"{Api}/admin/canned-responses")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"{Api}/admin/support/sla-policies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"{Api}/admin/help/categories")).StatusCode);

        // Manager: writes and canned / SLA, but no read of the queue without support.view.
        foreach (var (path, body) in writes.Take(1).Concat(writes.Skip(5)))
        {
            Assert.Equal(HttpStatusCode.Created, (await manager.PostAsJsonAsync($"{Api}/admin/support/{path}", body)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await manager.PostAsJsonAsync($"{Api}/admin/support/tickets/{id}/priority", new { priority = "high" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"{Api}/admin/canned-responses")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync($"{Api}/admin/support/sla-policies")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"{Api}/admin/support/tickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"{Api}/admin/support/summary")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync($"{Api}/admin/help/articles")).StatusCode);

        // Help editors work on the help centre only; riders and anonymous callers are refused.
        Assert.Equal(HttpStatusCode.OK, (await helper.GetAsync($"{Api}/admin/help/articles")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await helper.GetAsync($"{Api}/admin/support/tickets")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.Client.GetAsync($"{Api}/admin/support/tickets")).StatusCode);
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"{Api}/admin/support/tickets")).StatusCode);
    }
}
