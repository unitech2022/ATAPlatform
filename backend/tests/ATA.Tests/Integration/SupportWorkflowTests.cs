using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Support;
using ATA.Domain.Support;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SupportWorkflowTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    private Task<int> NotificationCountAsync(Guid userId, string type) =>
        fixture.Factory.WithDbAsync(db => db.Notifications.CountAsync(n => n.UserId == userId && n.Type == type));

    private static Task<HttpResponseMessage> Status(HttpClient admin, string id, string status, string? note = null) =>
        SupportFlow.AdminPostAsync(admin, id, "status", new { status, note });

    private async Task<(string Id, string Number)> TicketAsync(HttpClient client, string type = "other")
    {
        var ticket = await SupportFlow.CreateTicketAsync(client, type);
        return (Str(ticket, "id"), Str(ticket, "ticketNumber"));
    }

    [Fact]
    public async Task Sla_state_marks_due_soon_and_breached_tickets_and_the_queue_filters_them()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var urgent = await TicketAsync(passenger.Client, "safety");
        var normal = await TicketAsync(passenger.Client, "other");

        async Task<List<string>> IdsAsync(string sla)
        {
            var page = await (await admin.GetAsync($"{Api}/admin/support/tickets?sla={sla}&pageSize=100")).ReadJsonAsync();
            return page.GetProperty("items").EnumerateArray().Select(i => Str(i, "id")).ToList();
        }

        async Task<string> StateAsync(string id) => Str(await SupportFlow.AdminTicketAsync(admin, id), "slaState");

        // The urgent ticket is due in 15 minutes (within the 30-minute "due soon" window); the normal one in 4 hours.
        Assert.Equal("due_soon", await StateAsync(urgent.Id));
        Assert.Equal("ok", await StateAsync(normal.Id));
        Assert.Contains(urgent.Id, await IdsAsync("due_soon"));
        Assert.DoesNotContain(normal.Id, await IdsAsync("due_soon"));
        Assert.DoesNotContain(urgent.Id, await IdsAsync("breached"));
        var rows = (await (await admin.GetAsync($"{Api}/admin/support/tickets?pageSize=100")).ReadJsonAsync()).GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("due_soon", Str(rows.First(r => Str(r, "id") == urgent.Id), "slaState"));
        Assert.Equal("ok", Str(rows.First(r => Str(r, "id") == normal.Id), "slaState"));

        // Past the first-response deadline without an agent answer the ticket is breached.
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(16));
        Assert.Equal("breached", await StateAsync(urgent.Id));
        Assert.Contains(urgent.Id, await IdsAsync("breached"));
        Assert.DoesNotContain(normal.Id, await IdsAsync("breached"));
        var summary = await (await admin.GetAsync($"{Api}/admin/support/summary")).ReadJsonAsync();
        Assert.True(summary.GetProperty("breachingFirstResponse").GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Number, summary.GetProperty("open").ValueKind);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"{Api}/admin/support/tickets?sla=late")).StatusCode);

        // An agent answer ends the first-response breach (the resolution deadline is 4 hours away).
        (await SupportFlow.AdminPostAsync(admin, urgent.Id, "messages", new { body = "نتابع الأمر", isInternal = false })).EnsureSuccessStatusCode();
        Assert.Equal("ok", await StateAsync(urgent.Id));
        Assert.DoesNotContain(urgent.Id, await IdsAsync("breached"));

        // A resolved ticket no longer counts against the SLA at all.
        (await Status(admin, normal.Id, "resolved")).EnsureSuccessStatusCode();
        Assert.Equal("ok", await StateAsync(normal.Id));
    }

    [Fact]
    public async Task Waiting_for_the_user_pauses_the_sla_clock_and_the_users_reply_adds_the_paused_time_to_the_resolution_deadline()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var (id, number) = await TicketAsync(passenger.Client);
        var created = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(created.CreatedAt.AddMinutes(2880), created.ResolutionDueAt);

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(10));
        var pendingAt = fixture.Factory.Clock.UtcNow;
        var pending = await Status(admin, id, "pending_user", "أرسل صورة الإيصال من فضلك");
        Assert.Equal(HttpStatusCode.OK, pending.StatusCode);
        var pendingBody = await pending.ReadJsonAsync();
        Assert.Equal("pending_user", Str(pendingBody, "status"));
        var paused = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(pendingAt, paused.SlaPausedAt);
        Assert.Equal(0, paused.SlaPausedSeconds);
        Assert.Equal(created.ResolutionDueAt, paused.ResolutionDueAt);
        // The note is a public agent message: the first response, unread for the user, with support.status (not support.reply).
        Assert.Equal(pendingAt, paused.FirstResponseAt);
        Assert.Equal(1, paused.UnreadByUser);
        var userView = await SupportFlow.UserTicketAsync(passenger.Client, id);
        Assert.Equal("أرسل صورة الإيصال من فضلك", Str(userView.GetProperty("messages")[1], "body"));
        Assert.Equal(1, await NotificationCountAsync(passenger.UserId, "support.status"));
        Assert.Equal(0, await NotificationCountAsync(passenger.UserId, "support.reply"));
        var statusNotice = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().FirstAsync(n => n.UserId == passenger.UserId && n.Type == "support.status"));
        Assert.Contains(number, statusNotice.BodyAr);
        Assert.Contains("بانتظار ردك", statusNotice.BodyAr);
        Assert.Equal($"ata://support/tickets/{id}", JsonDocument.Parse(statusNotice.Data!).RootElement.GetProperty("deepLink").GetString());

        // The user answers 30 minutes later: the clock resumes and the deadline moves by the time spent waiting.
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(30));
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "هذه صورة الإيصال" })).EnsureSuccessStatusCode();
        var resumed = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportTicketStatus.Open, resumed.Status);
        Assert.Null(resumed.SlaPausedAt);
        Assert.Equal(1800, resumed.SlaPausedSeconds);
        Assert.Equal(created.ResolutionDueAt.AddSeconds(1800), resumed.ResolutionDueAt);
        Assert.Equal(resumed.CreatedAt.AddMinutes(2880).AddSeconds(1800), resumed.ResolutionDueAt);

        // A priority change keeps the paused time: both deadlines come from the new policy (created + minutes, + paused seconds for the resolution).
        var urgent = await SupportFlow.AdminPostAsync(admin, id, "priority", new { priority = "urgent" });
        Assert.Equal(HttpStatusCode.OK, urgent.StatusCode);
        var reprioritised = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportPriority.Urgent, reprioritised.Priority);
        Assert.Equal(reprioritised.CreatedAt.AddMinutes(15), reprioritised.FirstResponseDueAt);
        Assert.Equal(reprioritised.CreatedAt.AddMinutes(240).AddSeconds(1800), reprioritised.ResolutionDueAt);
        (await SupportFlow.AdminPostAsync(admin, id, "priority", new { priority = "low" })).EnsureSuccessStatusCode();
        var low = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(low.CreatedAt.AddMinutes(1440), low.FirstResponseDueAt);
        Assert.Equal(low.CreatedAt.AddMinutes(4320).AddSeconds(1800), low.ResolutionDueAt);
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "support_ticket.priority" && a.EntityId == Guid.Parse(id))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await SupportFlow.AdminPostAsync(admin, id, "priority", new { })).StatusCode);

        // An assigned ticket goes back to in_progress when the user answers.
        var (assignedId, _) = await TicketAsync(passenger.Client);
        var agentId = await SafetyFlow.AdminUserIdAsync(fixture);
        (await SupportFlow.AdminPostAsync(admin, assignedId, "assign", new { userId = agentId })).EnsureSuccessStatusCode();
        (await Status(admin, assignedId, "pending_user")).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{assignedId}/messages", new { body = "رد" })).EnsureSuccessStatusCode();
        Assert.Equal(SupportTicketStatus.InProgress, (await SupportFlow.TicketAsync(fixture, assignedId)).Status);
    }

    [Fact]
    public async Task A_ticket_that_waits_for_the_user_does_not_breach_while_paused_and_breaches_after_the_reply_when_the_deadline_passes()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var (id, _) = await TicketAsync(passenger.Client);
        var now = fixture.Factory.Clock.UtcNow;
        await SupportFlow.UpdateTicketAsync(fixture, id, t => t.ResolutionDueAt = now.AddMinutes(5));

        (await Status(admin, id, "pending_user")).EnsureSuccessStatusCode();
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(20));
        var whilePaused = await SupportFlow.AdminTicketAsync(admin, id);
        Assert.Equal("due_soon", Str(whilePaused, "slaState"));
        Assert.NotEqual(JsonValueKind.Null, whilePaused.GetProperty("slaPausedAt").ValueKind);
        var breached = await (await admin.GetAsync($"{Api}/admin/support/tickets?sla=breached&pageSize=100")).ReadJsonAsync();
        Assert.DoesNotContain(breached.GetProperty("items").EnumerateArray(), i => Str(i, "id") == id);

        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "رد" })).EnsureSuccessStatusCode();
        var resumed = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(1200, resumed.SlaPausedSeconds);
        Assert.Equal(now.AddMinutes(5).AddSeconds(1200), resumed.ResolutionDueAt);
        Assert.Equal("due_soon", Str(await SupportFlow.AdminTicketAsync(admin, id), "slaState"));
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal("breached", Str(await SupportFlow.AdminTicketAsync(admin, id), "slaState"));
        var summary = await (await admin.GetAsync($"{Api}/admin/support/summary")).ReadJsonAsync();
        Assert.True(summary.GetProperty("breachingResolution").GetInt32() >= 1);
    }

    [Fact]
    public async Task The_status_machine_follows_assignment_replies_resolution_reopening_and_the_final_closed_state()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var agentId = await SafetyFlow.AdminUserIdAsync(fixture);
        var (id, _) = await TicketAsync(passenger.Client);

        // Assignment: an active admin only; the first assignee moves open → in_progress; null unassigns.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await SupportFlow.AdminPostAsync(admin, id, "assign", new { userId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await SupportFlow.AdminPostAsync(admin, id, "assign", new { userId = passenger.UserId })).StatusCode);
        var assigned = await (await SupportFlow.AdminPostAsync(admin, id, "assign", new { userId = agentId })).ReadJsonAsync();
        Assert.Equal("in_progress", Str(assigned, "status"));
        Assert.Equal(agentId.ToString(), Str(assigned, "assignedToUserId"));
        Assert.NotEqual(JsonValueKind.Null, assigned.GetProperty("assignedAt").ValueKind);
        var unassigned = await (await SupportFlow.AdminPostAsync(admin, id, "assign", new { userId = (Guid?)null })).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, unassigned.GetProperty("assignedToUserId").ValueKind);
        Assert.Equal("in_progress", Str(unassigned, "status"));
        (await SupportFlow.AdminPostAsync(admin, id, "assign", new { userId = agentId })).EnsureSuccessStatusCode();

        // Invalid or repeated transitions.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Status(admin, id, "open")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Status(admin, id, "bogus")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await SupportFlow.AdminPostAsync(admin, id, "status", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Status(admin, id, "in_progress")).StatusCode);

        // pending_user, the user's answer, resolution (a public note), the user reopens by replying.
        (await Status(admin, id, "pending_user", "نحتاج معلومات")).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "المعلومات" })).EnsureSuccessStatusCode();
        Assert.Equal(SupportTicketStatus.InProgress, (await SupportFlow.TicketAsync(fixture, id)).Status);
        var resolvedAt = fixture.Factory.Clock.UtcNow;
        var resolved = await (await Status(admin, id, "resolved", "تم الحل، نأمل أن تكون راضياً")).ReadJsonAsync();
        Assert.Equal("resolved", Str(resolved, "status"));
        var row = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(resolvedAt, row.ResolvedAt);
        Assert.Equal(2, await NotificationCountAsync(passenger.UserId, "support.status"));
        Assert.Equal(0, await NotificationCountAsync(passenger.UserId, "support.reply"));
        var userDetail = await SupportFlow.UserTicketAsync(passenger.Client, id);
        Assert.True(userDetail.GetProperty("canRate").GetBoolean());
        Assert.True(userDetail.GetProperty("canReply").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, userDetail.GetProperty("resolvedAt").ValueKind);
        Assert.Equal("تم الحل، نأمل أن تكون راضياً", Str(userDetail.GetProperty("messages").EnumerateArray().Last(), "body"));

        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "لم تُحل المشكلة" })).EnsureSuccessStatusCode();
        var reopened = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportTicketStatus.InProgress, reopened.Status);
        Assert.Null(reopened.ResolvedAt);
        Assert.False((await SupportFlow.UserTicketAsync(passenger.Client, id)).GetProperty("canRate").GetBoolean());

        // An agent can reopen a resolved ticket too; a note with in_progress / closed is internal.
        (await Status(admin, id, "resolved")).EnsureSuccessStatusCode();
        var back = await Status(admin, id, "in_progress", "ملاحظة داخلية");
        Assert.Equal(HttpStatusCode.OK, back.StatusCode);
        Assert.Null((await SupportFlow.TicketAsync(fixture, id)).ResolvedAt);
        Assert.DoesNotContain("ملاحظة داخلية", (await SupportFlow.UserTicketAsync(passenger.Client, id)).ToString());
        Assert.Contains("ملاحظة داخلية", (await SupportFlow.AdminTicketAsync(admin, id)).ToString());

        // Closed is final.
        (await SupportFlow.AdminPostAsync(admin, id, "priority", new { priority = "high" })).EnsureSuccessStatusCode();
        (await Status(admin, id, "resolved")).EnsureSuccessStatusCode();
        var closedAt = fixture.Factory.Clock.UtcNow;
        (await Status(admin, id, "closed", "إغلاق")).EnsureSuccessStatusCode();
        var closed = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportTicketStatus.Closed, closed.Status);
        Assert.Equal(closedAt, closed.ClosedAt);
        Assert.NotNull(closed.ResolvedAt);
        var statusNotices = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().Where(n => n.UserId == passenger.UserId && n.Type == "support.status").Select(n => n.BodyAr).ToListAsync());
        Assert.Contains(statusNotices, body => body.Contains("مغلقة"));

        var userReply = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "أريد المتابعة" });
        Assert.Equal(HttpStatusCode.Conflict, userReply.StatusCode);
        Assert.Equal("ticket_closed", await userReply.ErrorCodeAsync());
        var agentReply = await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "رد", isInternal = false });
        Assert.Equal(HttpStatusCode.Conflict, agentReply.StatusCode);
        Assert.Equal("ticket_closed", await agentReply.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.Created, (await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "ملاحظة بعد الإغلاق", isInternal = true })).StatusCode);
        foreach (var (action, body) in new (string, object)[] { ("status", new { status = "in_progress" }), ("priority", new { priority = "high" }), ("assign", new { userId = agentId }), ("type", new { type = "account" }) })
        {
            var blocked = await SupportFlow.AdminPostAsync(admin, id, action, body);
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("ticket_closed", await blocked.ErrorCodeAsync());
        }

        var userView = await SupportFlow.UserTicketAsync(passenger.Client, id);
        Assert.False(userView.GetProperty("canReply").GetBoolean());
        Assert.True(userView.GetProperty("canRate").GetBoolean());
        var open = await (await passenger.Client.GetAsync($"{Api}/support/tickets?status=open")).ReadJsonAsync();
        Assert.DoesNotContain(open.GetProperty("items").EnumerateArray(), i => Str(i, "id") == id);
        var closedList = await (await passenger.Client.GetAsync($"{Api}/support/tickets?status=closed")).ReadJsonAsync();
        Assert.Contains(closedList.GetProperty("items").EnumerateArray(), i => Str(i, "id") == id);
        // A new ticket can be opened instead.
        Assert.Equal("open", Str(await SupportFlow.CreateTicketAsync(passenger.Client), "status"));

        // Type changes need the trip the new type requires; every state change is audited.
        var (typed, _) = await TicketAsync(passenger.Client);
        var needsTrip = await SupportFlow.AdminPostAsync(admin, typed, "type", new { type = "trip_issue" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, needsTrip.StatusCode);
        var changed = await (await SupportFlow.AdminPostAsync(admin, typed, "type", new { type = "account" })).ReadJsonAsync();
        Assert.Equal("account", Str(changed, "type"));
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "support_ticket").Select(a => a.Action).ToListAsync());
        foreach (var action in new[] { "support_ticket.assign", "support_ticket.status", "support_ticket.priority", "support_ticket.type" })
        {
            Assert.Contains(action, actions);
        }
    }

    [Fact]
    public async Task Resolved_tickets_close_automatically_after_three_days_without_a_reply()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var (quiet, _) = await TicketAsync(passenger.Client);
        var (answered, _) = await TicketAsync(passenger.Client);
        var (working, _) = await TicketAsync(passenger.Client);
        (await Status(admin, quiet, "resolved", "تم")).EnsureSuccessStatusCode();
        (await Status(admin, answered, "resolved", "تم")).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{answered}/messages", new { body = "ما زالت المشكلة" })).EnsureSuccessStatusCode();
        (await Status(admin, working, "in_progress")).EnsureSuccessStatusCode();
        var now = fixture.Factory.Clock.UtcNow;

        await SupportFlow.UpdateTicketAsync(fixture, quiet, t => t.ResolvedAt = now.AddDays(-3).AddMinutes(1));
        Assert.Equal(0, await fixture.Factory.RunSupportAutoCloseAsync());
        Assert.Equal(SupportTicketStatus.Resolved, (await SupportFlow.TicketAsync(fixture, quiet)).Status);

        await SupportFlow.UpdateTicketAsync(fixture, quiet, t => t.ResolvedAt = now.AddDays(-3));
        var before = await NotificationCountAsync(passenger.UserId, "support.status");
        Assert.Equal(1, await fixture.Factory.RunSupportAutoCloseAsync());
        var closed = await SupportFlow.TicketAsync(fixture, quiet);
        Assert.Equal(SupportTicketStatus.Closed, closed.Status);
        Assert.Equal(now, closed.ClosedAt);
        Assert.Equal(before + 1, await NotificationCountAsync(passenger.UserId, "support.status"));
        Assert.Contains(fixture.Realtime.AdminUpdates, u => u.TicketId == Guid.Parse(quiet) && u.Status == SupportTicketStatus.Closed);
        Assert.Equal(SupportTicketStatus.Open, (await SupportFlow.TicketAsync(fixture, answered)).Status);
        Assert.Equal(SupportTicketStatus.InProgress, (await SupportFlow.TicketAsync(fixture, working)).Status);
        Assert.Equal(0, await fixture.Factory.RunSupportAutoCloseAsync());

        // The user's reply to the closed ticket now answers 409 and may still rate it.
        var reply = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{quiet}/messages", new { body = "متأخر" });
        Assert.Equal("ticket_closed", await reply.ErrorCodeAsync());
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{quiet}/csat", new { score = 4 })).StatusCode);
    }

    [Fact]
    public async Task Csat_is_accepted_once_after_resolution_with_a_score_from_one_to_five()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var stranger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var (id, _) = await TicketAsync(passenger.Client);

        var early = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/csat", new { score = 5 });
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal("conflict", await early.ErrorCodeAsync());

        (await Status(admin, id, "resolved", "تم")).EnsureSuccessStatusCode();
        foreach (var bad in new object[] { new { score = 0 }, new { score = 6 }, new { }, new { score = 3, comment = new string('ش', 501) } })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/csat", bad)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/csat", new { score = 5 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/csat", new { score = 4, comment = "خدمة جيدة" })).StatusCode);
        var twice = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/csat", new { score = 1 });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        var detail = await SupportFlow.UserTicketAsync(passenger.Client, id);
        Assert.Equal(4, detail.GetProperty("csatScore").GetInt32());
        Assert.False(detail.GetProperty("canRate").GetBoolean());
        var adminDetail = await SupportFlow.AdminTicketAsync(admin, id);
        Assert.Equal(4, adminDetail.GetProperty("csatScore").GetInt32());
        Assert.Equal("خدمة جيدة", Str(adminDetail, "csatComment"));
    }

    [Fact]
    public async Task The_sla_monitor_broadcasts_each_state_change_of_an_active_ticket_once()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();
        var (id, _) = await TicketAsync(passenger.Client, "safety");
        var ticketId = Guid.Parse(id);
        IEnumerable<SupportTicketAdminEvent> Broadcasts() => fixture.Realtime.AdminUpdates.Where(u => u.TicketId == ticketId && u.SlaState != null);

        // Urgent: due in 15 minutes, so the first pass announces due_soon; the next pass is silent.
        await fixture.Factory.RunSupportSlaMonitorAsync();
        Assert.Equal([SlaState.DueSoon], Broadcasts().Select(b => b.SlaState!.Value).ToArray());
        await fixture.Factory.RunSupportSlaMonitorAsync();
        Assert.Single(Broadcasts());

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(16));
        await fixture.Factory.RunSupportSlaMonitorAsync();
        Assert.Equal([SlaState.DueSoon, SlaState.Breached], Broadcasts().Select(b => b.SlaState!.Value).ToArray());
        Assert.Equal(SupportPriority.Urgent, Broadcasts().Last().Priority);

        // Answered: back to ok (one more broadcast), then nothing once it is resolved.
        (await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "نتابع", isInternal = false })).EnsureSuccessStatusCode();
        await fixture.Factory.RunSupportSlaMonitorAsync();
        Assert.Equal(SlaState.Ok, Broadcasts().Last().SlaState);
        var count = Broadcasts().Count();
        (await Status(admin, id, "resolved")).EnsureSuccessStatusCode();
        await fixture.Factory.RunSupportSlaMonitorAsync();
        await fixture.Factory.RunSupportSlaMonitorAsync();
        Assert.Equal(count, Broadcasts().Count());
    }
}
