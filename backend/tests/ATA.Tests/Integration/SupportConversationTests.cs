using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Support;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SupportConversationTests(SupportFixture fixture) : IClassFixture<SupportFixture>
{
    private const string Api = SupportFlow.Base;

    private static string Str(JsonElement element, string name) => element.GetProperty(name).GetString()!;

    private Task<int> NotificationCountAsync(Guid userId, string type) =>
        fixture.Factory.WithDbAsync(db => db.Notifications.CountAsync(n => n.UserId == userId && n.Type == type));

    [Fact]
    public async Task Internal_notes_are_hidden_agent_replies_notify_and_count_unread_and_the_admins_hear_user_messages()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(0));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "trip_issue", ride.TripId, "مشكلة", "تفاصيل المشكلة");
        var id = Str(ticket, "id");
        var admin = await fixture.LoginAdminAsync();
        var agentId = await SafetyFlow.AdminUserIdAsync(fixture);

        // An internal note: invisible to the user, no notification, no status or first-response change.
        var note = await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "ملاحظة داخلية: نتحقق من المسار", isInternal = true });
        Assert.Equal(HttpStatusCode.Created, note.StatusCode);
        var noteBody = await note.ReadJsonAsync();
        Assert.True(noteBody.GetProperty("isInternal").GetBoolean());
        Assert.Equal(agentId.ToString(), Str(noteBody, "authorUserId"));
        Assert.Single((await SupportFlow.UserTicketAsync(ride.Passenger.Client, id)).GetProperty("messages").EnumerateArray());
        var afterNote = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportTicketStatus.Open, afterNote.Status);
        Assert.Null(afterNote.FirstResponseAt);
        Assert.Equal(SupportAuthorRole.User, afterNote.LastMessageBy);
        Assert.Equal(0, afterNote.UnreadByUser);
        Assert.Equal(0, await NotificationCountAsync(ride.Passenger.UserId, "support.reply"));

        // The first public reply: first response, in_progress, unread 1, support.reply with the ticket deep link.
        var now = fixture.Factory.Clock.UtcNow;
        var reply = await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "راجعنا الرحلة وسنعوّضك", isInternal = false });
        Assert.Equal(HttpStatusCode.Created, reply.StatusCode);
        var replied = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(SupportTicketStatus.InProgress, replied.Status);
        Assert.Equal(now, replied.FirstResponseAt);
        Assert.Equal(1, replied.UnreadByUser);
        Assert.Equal(SupportAuthorRole.Agent, replied.LastMessageBy);
        var notification = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().FirstAsync(n => n.UserId == ride.Passenger.UserId && n.Type == "support.reply"));
        Assert.Equal($"ata://support/tickets/{id}", JsonDocument.Parse(notification.Data!).RootElement.GetProperty("deepLink").GetString());
        Assert.Contains("راجعنا الرحلة", notification.BodyAr);
        Assert.Contains(Str(ticket, "ticketNumber"), notification.BodyAr);
        Assert.Contains(fixture.Realtime.UserUpdates, u => u.UserId == ride.Passenger.UserId && u.Event.TicketId == Guid.Parse(id) && u.Event.Unread == 1);
        Assert.Contains(fixture.Realtime.AdminUpdates, u => u.TicketId == Guid.Parse(id) && u.LastMessageBy == SupportAuthorRole.Agent && u.Status == SupportTicketStatus.InProgress);

        var list = await (await ride.Passenger.Client.GetAsync($"{Api}/support/tickets")).ReadJsonAsync();
        Assert.Equal(1, list.GetProperty("items").EnumerateArray().First(i => Str(i, "id") == id).GetProperty("unread").GetInt32());

        // A second reply raises the counter; the first response stays.
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        (await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "تمت الإضافة إلى محفظتك", isInternal = false })).EnsureSuccessStatusCode();
        var twice = await SupportFlow.TicketAsync(fixture, id);
        Assert.Equal(2, twice.UnreadByUser);
        Assert.Equal(now, twice.FirstResponseAt);
        Assert.Equal(2, await NotificationCountAsync(ride.Passenger.UserId, "support.reply"));

        // Opening the ticket shows the public conversation (agents appear as the team, in the caller's language) and clears the counter.
        using var english = fixture.CreateClient(ride.Passenger.Token, "en");
        var opened = await SupportFlow.UserTicketAsync(english, id);
        var messages = opened.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(3, messages.Count);
        Assert.Equal(["user", "agent", "agent"], messages.Select(m => Str(m, "authorRole")).ToArray());
        Assert.Equal("ATA Support", Str(messages[1], "authorName"));
        Assert.Equal("سارة أحمد", Str(messages[0], "authorName"));
        Assert.DoesNotContain("ملاحظة داخلية", opened.ToString());
        Assert.Equal(0, (await SupportFlow.TicketAsync(fixture, id)).UnreadByUser);
        Assert.Equal("فريق دعم ATA", Str((await SupportFlow.UserTicketAsync(ride.Passenger.Client, id)).GetProperty("messages")[1], "authorName"));
        Assert.Contains(fixture.Realtime.UserUpdates, u => u.UserId == ride.Passenger.UserId && u.Event.TicketId == Guid.Parse(id) && u.Event.Unread == 0);

        // The user answers: the admins are told over the hub; strangers cannot read or answer.
        var answer = await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "شكراً لكم" });
        Assert.Equal(HttpStatusCode.Created, answer.StatusCode);
        var answerBody = await answer.ReadJsonAsync();
        Assert.Equal("user", Str(answerBody, "authorRole"));
        Assert.Equal("شكراً لكم", Str(answerBody, "body"));
        Assert.Contains(fixture.Realtime.AdminUpdates, u => u.TicketId == Guid.Parse(id) && u.LastMessageBy == SupportAuthorRole.User);
        Assert.Equal(SupportAuthorRole.User, (await SupportFlow.TicketAsync(fixture, id)).LastMessageBy);
        var stranger = await SafetyFlow.PassengerAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await ride.Passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = " " })).StatusCode);

        // Agents see everything, in order, with the real agent names.
        var detail = await SupportFlow.AdminTicketAsync(admin, id);
        var all = detail.GetProperty("messages").EnumerateArray().ToList();
        Assert.Equal(5, all.Count);
        Assert.Equal(new[] { false, true, false, false, false }, all.Select(m => m.GetProperty("isInternal").GetBoolean()).ToArray());
        Assert.Equal(new[] { "user", "agent", "agent", "agent", "user" }, all.Select(m => Str(m, "authorRole")).ToArray());
        Assert.False(string.IsNullOrEmpty(Str(all[1], "authorName")));
    }

    [Fact]
    public async Task Canned_responses_fill_the_message_with_the_ticket_placeholders_in_the_requesters_language()
    {
        var ride = await SupportFlow.CompletedRideAsync(fixture, TripFlow.Area(1));
        var ticket = await SupportFlow.CreateTicketAsync(ride.Passenger.Client, "trip_issue", ride.TripId, "مشكلة", "تفاصيل");
        var id = Str(ticket, "id");
        var admin = await fixture.LoginAdminAsync();
        var canned = await admin.PostAsJsonAsync($"{Api}/admin/canned-responses", new
        {
            code = "hello_test", title = "ترحيب", bodyAr = "مرحباً {userName}، بخصوص {ticketNumber} للرحلة {tripNumber} {other}", bodyEn = "Hello {userName}, about {ticketNumber} trip {tripNumber}", ticketType = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, canned.StatusCode);

        var byCode = await SupportFlow.AdminPostAsync(admin, id, "messages", new { isInternal = false, cannedResponseCode = "hello_test" });
        Assert.Equal(HttpStatusCode.Created, byCode.StatusCode);
        var body = Str(await byCode.ReadJsonAsync(), "body");
        Assert.Equal($"مرحباً سارة أحمد، بخصوص {Str(ticket, "ticketNumber")} للرحلة {ride.TripNumber} {{other}}", body);

        // A typed body wins and its placeholders are filled too; an unknown code with no body is rejected.
        var typed = await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "شكراً {userName} — {ticketNumber}", isInternal = false, cannedResponseCode = "hello_test" });
        Assert.Equal($"شكراً سارة أحمد — {Str(ticket, "ticketNumber")}", Str(await typed.ReadJsonAsync(), "body"));
        var unknown = await SupportFlow.AdminPostAsync(admin, id, "messages", new { isInternal = false, cannedResponseCode = "nope" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        var empty = await SupportFlow.AdminPostAsync(admin, id, "messages", new { isInternal = false });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, empty.StatusCode);
    }

    [Fact]
    public async Task Attachments_are_limited_validated_and_readable_only_by_the_owner_the_agents_and_the_requester_of_public_messages()
    {
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var stranger = await SafetyFlow.PassengerAsync(fixture);
        var admin = await fixture.LoginAdminAsync();

        // Upload rules: jpg / png / pdf up to 10 MB.
        var upload = await SupportFlow.UploadRawAsync(passenger.Client, "proof.PNG");
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var uploaded = await upload.ReadJsonAsync();
        Assert.Equal("proof.PNG", Str(uploaded, "fileName"));
        Assert.Equal("image/png", Str(uploaded, "contentType"));
        Assert.Equal(7, uploaded.GetProperty("sizeBytes").GetInt64());
        Assert.Equal("application/pdf", Str(await (await SupportFlow.UploadRawAsync(passenger.Client, "receipt.pdf")).ReadJsonAsync(), "contentType"));
        var exe = await SupportFlow.UploadRawAsync(passenger.Client, "malware.exe");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, exe.StatusCode);
        Assert.Equal("unsupported_file_type", await exe.ErrorCodeAsync());
        var big = await SupportFlow.UploadRawAsync(passenger.Client, "big.jpg", new byte[10 * 1024 * 1024 + 1]);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, big.StatusCode);
        Assert.Equal("file_too_large", await big.ErrorCodeAsync());
        using var empty = new MultipartFormDataContent();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await passenger.Client.PostAsync($"{Api}/support/attachments", empty)).StatusCode);
        using var anonymous = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await SupportFlow.UploadRawAsync(anonymous, "proof.png")).StatusCode);

        // At most five attachments per message.
        var six = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            six.Add(await SupportFlow.UploadAsync(passenger.Client, $"p{i}.png"));
        }

        var tooMany = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets", new { type = "other", subject = "x", message = "y", fileIds = six });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooMany.StatusCode);
        var limitError = (await tooMany.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("attachment_limit", limitError.GetProperty("code").GetString());
        Assert.Equal(5, limitError.GetProperty("details").GetProperty("max").GetInt32());

        var ticket = await SupportFlow.CreateTicketAsync(passenger.Client, "other", null, "مرفقات", "مرفقاتي", fileIds: six.Take(5));
        var attachments = ticket.GetProperty("messages")[0].GetProperty("attachments").EnumerateArray().ToList();
        Assert.Equal(5, attachments.Count);
        Assert.Equal("image/png", Str(attachments[0], "contentType"));
        Assert.StartsWith("p", Str(attachments[0], "fileName"));
        var id = Str(ticket, "id");

        // Files must be the sender's own support uploads and used once.
        var again = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "مرة أخرى", fileIds = new[] { six[0] } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        var others = await SupportFlow.UploadAsync(stranger.Client);
        var foreign = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "ملف غيري", fileIds = new[] { others } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, foreign.StatusCode);
        var unknown = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "ملف مجهول", fileIds = new[] { Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        var replyFiles = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            replyFiles.Add(await SupportFlow.UploadAsync(passenger.Client, $"r{i}.png"));
        }

        var replyTooMany = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "كثير", fileIds = replyFiles });
        Assert.Equal("attachment_limit", await replyTooMany.ErrorCodeAsync());
        var replyOk = await passenger.Client.PostAsJsonAsync($"{Api}/support/tickets/{id}/messages", new { body = "خمسة", fileIds = replyFiles.Take(5) });
        Assert.Equal(HttpStatusCode.Created, replyOk.StatusCode);
        Assert.Equal(5, (await replyOk.ReadJsonAsync()).GetProperty("attachments").GetArrayLength());

        // Who may read what: the owner and the agents; the requester also reads what agents attach to public messages, but not to internal notes.
        var fileId = Str(attachments[0], "fileId");
        Assert.Equal(HttpStatusCode.OK, (await passenger.Client.GetAsync($"{Api}/files/{fileId}")).StatusCode);
        var asAgent = await admin.GetAsync($"{Api}/files/{fileId}");
        Assert.Equal(HttpStatusCode.OK, asAgent.StatusCode);
        Assert.Equal("image/png", asAgent.Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.GetAsync($"{Api}/files/{fileId}")).StatusCode);

        var publicFile = await SupportFlow.UploadAsync(admin, "answer.pdf");
        var internalFile = await SupportFlow.UploadAsync(admin, "internal.png");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.Client.GetAsync($"{Api}/files/{publicFile}")).StatusCode);
        (await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "مرفق الرد", isInternal = false, fileIds = new[] { publicFile } })).EnsureSuccessStatusCode();
        (await SupportFlow.AdminPostAsync(admin, id, "messages", new { body = "مرفق داخلي", isInternal = true, fileIds = new[] { internalFile } })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await passenger.Client.GetAsync($"{Api}/files/{publicFile}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.Client.GetAsync($"{Api}/files/{internalFile}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.GetAsync($"{Api}/files/{publicFile}")).StatusCode);

        // The agent sees the attachments in the admin detail; the requester sees only the public one.
        var adminDetail = await SupportFlow.AdminTicketAsync(admin, id);
        Assert.Equal(2, adminDetail.GetProperty("messages").EnumerateArray().Count(m => m.GetProperty("attachments").GetArrayLength() == 1));
        var userDetail = await SupportFlow.UserTicketAsync(passenger.Client, id);
        Assert.Equal(1, userDetail.GetProperty("messages").EnumerateArray().Count(m => m.GetProperty("authorRole").GetString() == "agent" && m.GetProperty("attachments").GetArrayLength() == 1));
    }
}
