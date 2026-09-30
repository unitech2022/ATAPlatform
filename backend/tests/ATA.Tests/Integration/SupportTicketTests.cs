using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Safety;
using ATA.Domain.Support;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SupportTicketTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    [Fact]
    public async Task Trip_types_require_a_trip_the_user_took_part_in()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(0));
        var stranger = await SafetyFlow.PassengerAsync(fixture);

        foreach (var type in new[] { "trip_issue", "payment_issue", "lost_item" })
        {
            var response = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type, subject = "موضوع", message = "رسالة" });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var error = (await response.ReadJsonAsync()).GetProperty("error");
            Assert.Equal("validation_failed", error.GetProperty("code").GetString());
            Assert.Equal("required", error.GetProperty("details").GetProperty("tripId").GetString());
        }

        var notParty = await stranger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "trip_issue", tripId = ride.TripId, subject = "موضوع", message = "رسالة" });
        Assert.Equal(HttpStatusCode.Forbidden, notParty.StatusCode);
        var unknown = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "trip_issue", tripId = Guid.NewGuid(), subject = "موضوع", message = "رسالة" });
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        // The account / other types need no trip.
        Assert.Equal("open", Str(await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "account"), "status"));
        Assert.Equal("other", Str(await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "other"), "type"));

        // Field validation.
        var blank = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "other", subject = " ", message = "" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blank.StatusCode);
        var details = (await blank.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        Assert.Equal("required", details.GetProperty("subject").GetString());
        Assert.Equal("required", details.GetProperty("message").GetString());
        var longSubject = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "other", subject = new string('س', 161), message = "x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, longSubject.StatusCode);
        var badType = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "nonsense", subject = "x", message = "x" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badType.StatusCode);
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync($"{Api}/support/tickets", new { type = "other", subject = "x", message = "x" })).StatusCode);

        // A trip issue of a party: the detail has the trip, one message and no dispute; the driver of the trip may open one too.
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "trip_issue", ride.TripId, "السائق سلك طريقاً أطول", "المسار كان أطول من المعتاد");
        Assert.Matches(@"^ST-\d{8}-\d{5}$", Str(ticket, "ticketNumber"));
        Assert.Equal("normal", Str(ticket, "priority"));
        Assert.Equal(ride.TripNumber, ticket.GetProperty("trip").GetProperty("tripNumber").GetString());
        Assert.NotEqual(JsonValueKind.Null, ticket.GetProperty("trip").GetProperty("completedAt").ValueKind);
        var messages = ticket.GetProperty("messages").EnumerateArray().ToList();
        Assert.Single(messages);
        Assert.Equal("user", Str(messages[0], "authorRole"));
        Assert.Equal("المسار كان أطول من المعتاد", Str(messages[0], "body"));
        Assert.True(ticket.GetProperty("canReply").GetBoolean());
        Assert.False(ticket.GetProperty("canRate").GetBoolean());
        Assert.Equal(JsonValueKind.Null, ticket.GetProperty("dispute").ValueKind);
        Assert.Equal(JsonValueKind.Null, ticket.GetProperty("csatScore").ValueKind);

        var driverTicket = await SupportFlow.CreateTicketAsync(ride.Driver.Client, "trip_issue", ride.TripId, "الراكب لم يحضر", "انتظرت كثيراً");
        var row = await SupportFlow.TicketAsync(fixture, Str(driverTicket, "id"));
        Assert.Equal(SupportRequesterRole.Driver, row.RequesterRole);
        Assert.Equal(ride.Driver.UserId, row.RequesterUserId);
        Assert.Equal(SupportRequesterRole.Passenger, (await SupportFlow.TicketAsync(fixture, Str(ticket, "id"))).RequesterRole);

        // Another user cannot see the ticket; the list shows only the caller's own ones with the trip number and the unread counter.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.GetAsync($"{Api}/support/tickets/{Str(ticket, "id")}")).StatusCode);
        var list = await (await ride.Passenger.Client.GetAsync($"{Api}/support/tickets")).ReadJsonAsync();
        Assert.Equal(3, list.GetProperty("total").GetInt32());
        var summary = list.GetProperty("items").EnumerateArray().First(i => Str(i, "id") == Str(ticket, "id"));
        Assert.Equal(ride.TripNumber, Str(summary, "tripNumber"));
        Assert.Equal(0, summary.GetProperty("unread").GetInt32());
        Assert.Equal("trip_issue", Str(summary, "type"));
        Assert.Equal(0, (await (await stranger.Client.GetAsync($"{Api}/support/tickets")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ride.Passenger.Client.GetAsync($"{Api}/support/tickets?status=bogus")).StatusCode);
    }

    [Fact]
    public async Task Default_priority_follows_the_type_and_the_sla_due_dates_follow_the_policy_of_the_priority()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(1));
        var expected = new Dictionary<string, (string Priority, int FirstResponse, int Resolution)>
        {
            ["safety"] = ("urgent", 15, 240),
            ["payment_issue"] = ("high", 60, 1440),
            ["trip_issue"] = ("normal", 240, 2880),
            ["lost_item"] = ("normal", 240, 2880),
            ["account"] = ("normal", 240, 2880),
            ["other"] = ("normal", 240, 2880),
        };
        foreach (var (type, rule) in expected)
        {
            var tripId = type is "payment_issue" or "trip_issue" or "lost_item" ? ride.TripId : null;
            var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, type, tripId);
            Assert.Equal(rule.Priority, Str(ticket, "priority"));
            var row = await SupportFlow.TicketAsync(fixture, Str(ticket, "id"));
            Assert.Equal(fixture.Factory.Clock.UtcNow, row.CreatedAt);
            Assert.Equal(row.CreatedAt.AddMinutes(rule.FirstResponse), row.FirstResponseDueAt);
            Assert.Equal(row.CreatedAt.AddMinutes(rule.Resolution), row.ResolutionDueAt);
            Assert.Equal(row.CreatedAt, row.LastMessageAt);
            Assert.Equal(SupportAuthorRole.User, row.LastMessageBy);
            Assert.Equal(0, row.UnreadByUser);
            Assert.Null(row.FirstResponseAt);
        }
    }

    [Fact]
    public async Task A_safety_ticket_creates_a_safety_case_linked_both_ways()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(2));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "safety", ride.TripId, "شعرت بعدم الأمان", "قاد السائق بسرعة عالية جداً");
        var ticketId = Guid.Parse(Str(ticket, "id"));
        Assert.Equal("urgent", Str(ticket, "priority"));

        var row = await SupportFlow.TicketAsync(fixture, ticketId.ToString());
        Assert.NotNull(row.SafetyCaseId);
        var safetyCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.AsNoTracking().FirstAsync(c => c.Id == row.SafetyCaseId));
        Assert.Equal(SafetyCaseType.SafetyReport, safetyCase.Type);
        Assert.Equal(SafetyCaseSource.Support, safetyCase.Source);
        Assert.Equal(SafetyPriority.High, safetyCase.Priority);
        Assert.Equal(SafetyCaseStatus.Open, safetyCase.Status);
        Assert.Equal(Guid.Parse(ride.TripId), safetyCase.TripId);
        Assert.Equal(ride.Passenger.UserId, safetyCase.ReporterUserId);
        Assert.Equal(SafetyReporterRole.Passenger, safetyCase.ReporterRole);
        Assert.Equal(ride.Driver.UserId, safetyCase.SubjectUserId);
        Assert.Equal("قاد السائق بسرعة عالية جداً", safetyCase.Description);
        Assert.Equal(ticketId, safetyCase.SupportTicketId);

        // The admin safety centre shows the ticket id; the ticket detail shows the linked case.
        var admin = await fixture.LoginAdminAsync();
        var caseDetail = await (await admin.GetAsync($"{Api}/admin/safety/cases/{safetyCase.Id}")).ReadJsonAsync();
        Assert.Equal(ticketId.ToString(), Str(caseDetail, "supportTicketId"));
        var ticketDetail = await SupportFlow.AdminTicketAsync(admin, ticketId.ToString());
        var linked = ticketDetail.GetProperty("linked").GetProperty("safetyCase");
        Assert.Equal(safetyCase.CaseNumber, Str(linked, "number"));
        Assert.Equal(safetyCase.Id.ToString(), Str(linked, "id"));
        Assert.Equal("open", Str(linked, "status"));
        Assert.Equal(safetyCase.Id.ToString(), Str(ticketDetail, "safetyCaseId"));
        Assert.Equal(JsonValueKind.Null, ticketDetail.GetProperty("linked").GetProperty("lostItemReport").ValueKind);

        // A safety ticket from a driver without a trip still opens a case (reporter role from the login).
        var driverTicket = await SupportFlow.CreateTicketAsync(ride.Driver.Client, "safety", null, "موقف مقلق", "تعرضت لموقف مقلق");
        var driverRow = await SupportFlow.TicketAsync(fixture, Str(driverTicket, "id"));
        var driverCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.AsNoTracking().FirstAsync(c => c.Id == driverRow.SafetyCaseId));
        Assert.Null(driverCase.TripId);
        Assert.Equal(SafetyReporterRole.Driver, driverCase.ReporterRole);
        Assert.Equal(SupportRequesterRole.Driver, driverRow.RequesterRole);
        Assert.Equal(driverRow.Id, driverCase.SupportTicketId);
    }

    [Fact]
    public async Task A_lost_item_ticket_creates_a_linked_report_and_a_lost_item_report_creates_a_linked_ticket()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(3));

        // Support route: the ticket creates the report (category from the optional lostItem object, phone defaulting to the rider's).
        var created = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new
        {
            type = "lost_item", tripId = ride.TripId, subject = "نسيت محفظتي", message = "محفظة سوداء على المقعد الخلفي", lostItem = new { itemCategory = "wallet" },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var ticketId = Str(await created.ReadJsonAsync(), "id");
        var ticket = await SupportFlow.TicketAsync(fixture, ticketId);
        Assert.NotNull(ticket.LostItemReportId);
        var report = await fixture.Factory.WithDbAsync(db => db.LostItemReports.AsNoTracking().FirstAsync(r => r.Id == ticket.LostItemReportId));
        Assert.Equal(Guid.Parse(ticketId), report.SupportTicketId);
        Assert.Equal(LostItemCategory.Wallet, report.ItemCategory);
        Assert.Equal(ride.Passenger.Phone, report.ContactPhone);
        Assert.Equal("محفظة سوداء على المقعد الخلفي", report.Description);
        Assert.Equal(ride.Driver.UserId, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == report.DriverId).Select(d => d.UserId).FirstAsync()));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Driver.UserId && n.Type == "lost_item.reported")));
        Assert.Equal(SupportRequesterRole.Passenger, ticket.RequesterRole);

        // A driver's lost_item ticket is only a ticket: lost item reports belong to riders.
        var before = await fixture.Factory.WithDbAsync(db => db.LostItemReports.CountAsync());
        var driverTicket = await SupportFlow.CreateTicketAsync(ride.Driver.Client, "lost_item", ride.TripId, "غرض نسيه الراكب", "وجدت جواله");
        Assert.Null((await SupportFlow.TicketAsync(fixture, Str(driverTicket, "id"))).LostItemReportId);
        Assert.Equal(before, await fixture.Factory.WithDbAsync(db => db.LostItemReports.CountAsync()));

        // F12 route: the report opens a linked ticket (type lost_item, normal priority, the description as first message).
        var f12 = await (await ride.Passenger.Client.PostAsJsonAsync($"{Api}/passenger/trips/{ride.TripId}/lost-items", new { itemCategory = "phone", description = "جوال أزرق" })).ReadJsonAsync();
        var linkedTicketId = Str(f12, "supportTicketId");
        var reportId = Str(f12, "id");
        var linked = await SupportFlow.TicketAsync(fixture, linkedTicketId);
        Assert.Equal(SupportTicketType.LostItem, linked.Type);
        Assert.Equal(SupportPriority.Normal, linked.Priority);
        Assert.Equal(SupportRequesterRole.Passenger, linked.RequesterRole);
        Assert.Equal(ride.Passenger.UserId, linked.RequesterUserId);
        Assert.Equal(Guid.Parse(ride.TripId), linked.TripId);
        Assert.Equal(Guid.Parse(reportId), linked.LostItemReportId);
        Assert.Equal(SupportChannel.App, linked.Channel);
        var detail = await SupportFlow.UserTicketAsync(ride.Passenger.Client, linkedTicketId);
        Assert.Equal("جوال أزرق", Str(detail.GetProperty("messages")[0], "body"));
        Assert.Contains("جوال", Str(detail, "subject"));
        var mine = await (await ride.Passenger.Client.GetAsync($"{Api}/passenger/lost-items")).ReadJsonAsync();
        Assert.Equal(linkedTicketId, Str(mine.GetProperty("items").EnumerateArray().First(i => Str(i, "id") == reportId), "supportTicketId"));
        var admin = await fixture.LoginAdminAsync();
        var adminDetail = await SupportFlow.AdminTicketAsync(admin, linkedTicketId);
        Assert.Equal(f12.GetProperty("reportNumber").GetString(), Str(adminDetail.GetProperty("linked").GetProperty("lostItemReport"), "number"));
        var adminReport = (await (await admin.GetAsync($"{Api}/admin/lost-items?search={ride.TripNumber}")).ReadJsonAsync()).GetProperty("items").EnumerateArray().First(i => Str(i, "id") == reportId);
        Assert.Equal(linkedTicketId, Str(adminReport, "supportTicketId"));

        // The driver's answer: lost_item.update carries the ticket deep link and a system line lands in the ticket (status untouched).
        (await ride.Driver.Client.PostAsJsonAsync($"{Api}/driver/lost-items/{reportId}/respond", new { found = true, note = "موجود" })).EnsureSuccessStatusCode();
        var notification = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().FirstAsync(n => n.UserId == ride.Passenger.UserId && n.Type == "lost_item.update"));
        Assert.Equal($"ata://support/tickets/{linkedTicketId}", JsonDocument.Parse(notification.Data!).RootElement.GetProperty("deepLink").GetString());
        var afterAnswer = await SupportFlow.UserTicketAsync(ride.Passenger.Client, linkedTicketId);
        var lines = afterAnswer.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(2, lines.Count);
        Assert.Equal("system", Str(lines[1], "authorRole"));
        Assert.Contains("عثر الكابتن على الغرض", Str(lines[1], "body"));
        Assert.Equal("open", Str(afterAnswer, "status"));

        (await admin.PatchAsJsonAsync($"{Api}/admin/lost-items/{reportId}", new { status = "returned", note = "سُلّم" })).EnsureSuccessStatusCode();
        Assert.Equal(3, (await SupportFlow.UserTicketAsync(ride.Passenger.Client, linkedTicketId)).GetProperty("messages").GetArrayLength());
        Assert.Contains(fixture.Realtime.UserUpdates, u => u.UserId == ride.Passenger.UserId && u.Event.TicketId == Guid.Parse(linkedTicketId));
    }

    [Fact]
    public async Task Lost_item_tickets_follow_the_report_window_and_need_a_completed_trip()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(4));
        await fixture.Factory.WithDbAsync(async db =>
        {
            var trip = await db.Trips.FirstAsync(t => t.Id == Guid.Parse(ride.TripId));
            trip.CompletedAt = fixture.Factory.Clock.UtcNow.AddDays(-8);
            await db.SaveChangesAsync();
            return true;
        });
        var late = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "lost_item", tripId = ride.TripId, subject = "متأخر", message = "نسيت شيئاً" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, late.StatusCode);
        Assert.Equal("lost_item_window_closed", await late.ErrorCodeAsync());
        var viaF12 = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/passenger/trips/{ride.TripId}/lost-items", new { itemCategory = "bag", description = "حقيبة" });
        Assert.Equal("lost_item_window_closed", await viaF12.ErrorCodeAsync());
        Assert.Equal(0, await fixture.Factory.WithDbAsync(db => db.SupportTickets.CountAsync(t => t.TripId == Guid.Parse(ride.TripId))));

        // A trip issue has no such window; an unfinished trip cannot carry a lost item.
        Assert.Equal("open", Str(await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "trip_issue", ride.TripId), "status"));
        var next = await SafetyFlow.NextRideAsync(fixture, TripFlow.Area(4), ride.Passenger, ride.Driver, ride.DriverId);
        var unfinished = await next.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "lost_item", tripId = next.TripId, subject = "x", message = "y" });
        Assert.Equal(HttpStatusCode.Conflict, unfinished.StatusCode);
    }

    [Fact]
    public async Task New_tickets_are_announced_to_the_admins_over_the_hub_and_admin_created_phone_tickets_are_audited()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(5));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "trip_issue", ride.TripId, "مشكلة", "تفاصيل");
        var created = fixture.Realtime.Created.Single(c => c.Id == Guid.Parse(Str(ticket, "id")));
        Assert.Equal(Str(ticket, "ticketNumber"), created.TicketNumber);
        Assert.Equal(SupportTicketType.TripIssue, created.Type);
        Assert.Equal(SupportAuthorRole.User, created.LastMessageBy);
        Assert.Equal(ride.TripNumber, created.TripNumber);
        Assert.Equal(SupportChannel.App, created.Channel);

        // By phone: the agent opens the ticket for the caller (a party of the trip), with a priority and the phone channel.
        var admin = await fixture.LoginAdminAsync();
        var response = await admin.PostAsJsonAsync($"{Api}/admin/support/tickets", new
        {
            requesterUserId = ride.Passenger.UserId, type = "payment_issue", tripId = ride.TripId, subject = "اتصال هاتفي", message = "اتصلت الراكبة بخصوص الأجرة", priority = "urgent", channel = "phone",
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("phone", Str(body, "channel"));
        Assert.Equal("urgent", Str(body, "priority"));
        Assert.Equal(ride.Passenger.Phone, Str(body.GetProperty("requester"), "phoneNumber"));
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "support_ticket.create" && a.EntityId == Guid.Parse(Str(body, "id")))));
        // The caller sees it in the app; the first message is the caller's account of the call.
        var mine = await SupportFlow.UserTicketAsync(ride.Passenger.Client, Str(body, "id"));
        Assert.Equal("user", Str(mine.GetProperty("messages")[0], "authorRole"));

        var stranger = await SafetyFlow.PassengerAsync(fixture);
        var notParty = await admin.PostAsJsonAsync($"{Api}/admin/support/tickets", new { requesterUserId = stranger.UserId, type = "trip_issue", tripId = ride.TripId, subject = "x", message = "y" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notParty.StatusCode);
        var unknownUser = await admin.PostAsJsonAsync($"{Api}/admin/support/tickets", new { requesterUserId = Guid.NewGuid(), type = "other", subject = "x", message = "y" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownUser.StatusCode);
        var badChannel = await admin.PostAsJsonAsync($"{Api}/admin/support/tickets", new { requesterUserId = ride.Passenger.UserId, type = "other", subject = "x", message = "y", channel = "app" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badChannel.StatusCode);
    }
}
