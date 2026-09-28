using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SafetyTests(SafetyFixture fixture) : IClassFixture<SafetyFixture>
{
    [Fact]
    public async Task Share_link_lifecycle_public_page_hides_private_data_and_expires_after_the_trip()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture, "سارة أحمد");
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "محمد العتيبي");
        var created = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString()!;

        var tooEarly = await passenger.Client.PostAsJsonAsync($"/api/v1/safety/trips/{tripId}/shares", new { channel = "link" });
        Assert.Equal(HttpStatusCode.Conflict, tooEarly.StatusCode);

        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();

        var response = await passenger.Client.PostAsJsonAsync($"/api/v1/safety/trips/{tripId}/shares", new { channel = "link" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var share = (await response.ReadJsonAsync()).GetProperty("shares")[0];
        var url = share.GetProperty("url").GetString()!;
        Assert.StartsWith("https://ata.sa/t/", url);
        var token = url["https://ata.sa/t/".Length..];
        Assert.Equal(22, token.Length);
        Assert.Equal("link", share.GetProperty("channel").GetString());
        Assert.Equal(JsonValueKind.Null, share.GetProperty("expiresAt").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await driver.Client.PostAsJsonAsync($"/api/v1/safety/trips/{tripId}/shares", new { channel = "link" })).StatusCode);

        using var anonymous = fixture.CreateClient();
        var publicResponse = await anonymous.GetAsync($"/api/v1/public/trip-shares/{token}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        var raw = await publicResponse.Content.ReadAsStringAsync();
        var view = JsonDocument.Parse(raw).RootElement;
        Assert.Equal("driver_assigned", view.GetProperty("status").GetString());
        Assert.Equal("سارة", view.GetProperty("passengerFirstName").GetString());
        Assert.Equal("محمد", view.GetProperty("driver").GetProperty("firstName").GetString());
        Assert.Equal($"/api/v1/public/trip-shares/{token}/driver-photo", view.GetProperty("driver").GetProperty("photoUrl").GetString());
        Assert.Equal("Camry", view.GetProperty("vehicle").GetProperty("model").GetString());
        Assert.Equal("economy", view.GetProperty("rideCategory").GetProperty("code").GetString());
        Assert.Equal(10, view.GetProperty("refreshSeconds").GetInt32());
        Assert.Equal("pickup", view.GetProperty("etaTarget").GetString());
        Assert.True(view.GetProperty("route").GetProperty("planned").GetArrayLength() >= 2);
        Assert.DoesNotContain("+966", raw);
        Assert.DoesNotContain("العتيبي", raw);
        Assert.DoesNotContain("أحمد", raw);
        foreach (var hidden in new[] { "pin", "fare", "estimatedFare", "paymentMethod", "phoneMasked", "phoneNumber" })
        {
            Assert.False(view.TryGetProperty(hidden, out _), hidden);
        }

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync($"/api/v1/public/trip-shares/{token}/driver-photo")).StatusCode);
        (await anonymous.GetAsync($"/api/v1/public/trip-shares/{token}")).EnsureSuccessStatusCode();
        var list = await (await passenger.Client.GetAsync($"/api/v1/safety/trips/{tripId}/shares")).ReadJsonAsync();
        Assert.Equal(1, list[0].GetProperty("viewCount").GetInt32()); // counted once per 60 s per IP

        var unknown = await anonymous.GetAsync("/api/v1/public/trip-shares/AAAAAAAAAAAAAAAAAAAAAA");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("share_not_found", await unknown.ErrorCodeAsync());

        var second = (await (await passenger.Client.PostAsJsonAsync($"/api/v1/safety/trips/{tripId}/shares", new { channel = "link" })).ReadJsonAsync()).GetProperty("shares")[0];
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.DeleteAsync($"/api/v1/safety/shares/{second.GetProperty("id").GetString()}")).StatusCode);
        var revoked = await anonymous.GetAsync($"/api/v1/public/trip-shares/{second.GetProperty("url").GetString()!.Split('/')[^1]}");
        Assert.Equal(HttpStatusCode.Gone, revoked.StatusCode);
        Assert.Equal("share_expired", await revoked.ErrorCodeAsync());

        using var admin = await fixture.LoginAdminAsync();
        var adminShares = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/shares")).ReadJsonAsync();
        Assert.Equal(2, adminShares.GetArrayLength());

        var ride = new SafetyFlow.Ride(passenger, driver, Guid.Empty, tripId, created.GetProperty("tripNumber").GetString()!);
        await SafetyFlow.CompleteAsync(ride);
        var afterEnd = await (await anonymous.GetAsync($"/api/v1/public/trip-shares/{token}")).ReadJsonAsync();
        Assert.Equal("completed", afterEnd.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, afterEnd.GetProperty("expiresAt").ValueKind);
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(31));
        var expired = await anonymous.GetAsync($"/api/v1/public/trip-shares/{token}");
        Assert.Equal(HttpStatusCode.Gone, expired.StatusCode);
        Assert.Equal("share_expired", await expired.ErrorCodeAsync());
    }

    [Fact]
    public async Task Trusted_contacts_are_limited_to_five_unique_non_self_numbers_and_auto_share_on_assignment()
    {
        var area = TripFlow.Area(1);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var invalid = await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "x", phoneNumber = "12345" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("phone_invalid", await invalid.ErrorCodeAsync());
        var self = await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "أنا", phoneNumber = passenger.Phone });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, self.StatusCode);
        Assert.Equal("self", (await self.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("phoneNumber").GetString());

        var phones = Enumerable.Range(0, 6).Select(i => $"05{70000000 + i:D8}").ToList();
        var first = await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "أخي", phoneNumber = phones[0], relationship = "أخ", autoShare = true, notifyOnSos = true });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.ReadJsonAsync();
        Assert.Equal("+966570000000", firstBody.GetProperty("phoneNumber").GetString());
        Assert.True(firstBody.GetProperty("autoShare").GetBoolean());
        var duplicate = await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "مكرر", phoneNumber = "+966570000000" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("trusted_contact_exists", await duplicate.ErrorCodeAsync());
        for (var i = 1; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.Created, (await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = $"جهة {i}", phoneNumber = phones[i] })).StatusCode);
        }

        var sixth = await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "السادسة", phoneNumber = phones[5] });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, sixth.StatusCode);
        Assert.Equal("trusted_contacts_limit", await sixth.ErrorCodeAsync());

        var contacts = await (await passenger.Client.GetAsync("/api/v1/safety/trusted-contacts")).ReadJsonAsync();
        Assert.Equal(5, contacts.GetArrayLength());
        var lastId = contacts[4].GetProperty("id").GetString();
        var updated = await (await passenger.Client.PutAsJsonAsync($"/api/v1/safety/trusted-contacts/{lastId}", new { name = "أمي", phoneNumber = phones[4], relationship = "أم", autoShare = false, notifyOnSos = false })).ReadJsonAsync();
        Assert.Equal("أمي", updated.GetProperty("name").GetString());
        Assert.False(updated.GetProperty("notifyOnSos").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.DeleteAsync($"/api/v1/safety/trusted-contacts/{lastId}")).StatusCode);

        var ride = await SafetyFlow.AssignedRideAsync(fixture, area, passenger: passenger);
        var shares = await (await passenger.Client.GetAsync($"/api/v1/safety/trips/{ride.TripId}/shares")).ReadJsonAsync();
        var auto = Assert.Single(shares.EnumerateArray());
        Assert.Equal("auto", auto.GetProperty("channel").GetString());
        Assert.Equal("أخي", auto.GetProperty("trustedContactName").GetString());
        await fixture.Factory.RunNotificationWorkerAsync();
        Assert.Contains(fixture.Sms.Sent, m => m.Phone == "+966570000000" && m.Message.Contains(auto.GetProperty("url").GetString()!));

        // channel=sms creates one link per chosen contact and texts it.
        var otherId = contacts[1].GetProperty("id").GetString();
        var sms = await (await passenger.Client.PostAsJsonAsync($"/api/v1/safety/trips/{ride.TripId}/shares", new { channel = "sms", contactIds = new[] { otherId } })).ReadJsonAsync();
        Assert.Equal(otherId, sms.GetProperty("shares")[0].GetProperty("trustedContactId").GetString());
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.CountAsync(d => d.EventCode == "safety.trip_shared" && d.PhoneNumber == "+966570000001")));
    }

    [Fact]
    public async Task Sos_opens_a_critical_case_notifies_on_duty_ops_and_trusted_contacts_and_dedupes_repeated_presses()
    {
        var area = TripFlow.Area(2);
        var admin = await SafetyFlow.OnDutyAdminAsync(fixture);
        var adminUserId = await SafetyFlow.AdminUserIdAsync(fixture);
        using var offDuty = await PaymentFlow.SecondAdminAsync(fixture, "safety_off_duty");
        var offDutyUserId = await SafetyFlow.AdminUserIdAsync(fixture, "safety_off_duty");
        var adminToken = await AdminTokenAsync();
        await using var hub = TripFlow.Hub(fixture, adminToken);
        var opened = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>("SafetyCaseOpened", c => { if (c.GetProperty("type").GetString() == "sos") opened.TrySetResult(c); });
        await hub.StartAsync();

        var passenger = await SafetyFlow.PassengerAsync(fixture, "نورة سعد");
        (await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "أبي", phoneNumber = "0571111111", notifyOnSos = true })).EnsureSuccessStatusCode();
        (await passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "زميلة", phoneNumber = "0571111112", notifyOnSos = false })).EnsureSuccessStatusCode();
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area, passenger: passenger);

        var sos = await passenger.Client.PostAsJsonAsync("/api/v1/safety/sos", new { tripId = ride.TripId, lat = 24.55m, lng = 46.61m, accuracy = 12, note = "أشعر بالخطر", notifyTrustedContacts = true });
        Assert.Equal(HttpStatusCode.Created, sos.StatusCode);
        var body = await sos.ReadJsonAsync();
        var caseId = Guid.Parse(body.GetProperty("caseId").GetString()!);
        Assert.Matches(@"^SC-\d{8}-\d{4}$", body.GetProperty("caseNumber").GetString());
        Assert.Equal("open", body.GetProperty("status").GetString());
        Assert.Equal("911", body.GetProperty("emergencyNumber").GetString());
        Assert.Equal(1, body.GetProperty("contactsNotified").GetInt32());

        var safetyCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == caseId));
        Assert.Equal(SafetyPriority.Critical, safetyCase.Priority);
        Assert.Equal(SafetyCaseSource.RiderSos, safetyCase.Source);
        Assert.Equal(SafetyReporterRole.Passenger, safetyCase.ReporterRole);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AnyAsync(d => d.UserId == adminUserId && d.EventCode == NotificationTypes.SafetyAlert && d.Channel == NotificationChannel.Push)));
        Assert.False(await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AnyAsync(d => d.UserId == offDutyUserId && d.EventCode == NotificationTypes.SafetyAlert)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AnyAsync(d => d.PhoneNumber == "+966511111111" && d.EventCode == NotificationTypes.SafetyAlert && d.Channel == NotificationChannel.Sms)));
        await fixture.Factory.RunNotificationWorkerAsync();
        var contactSms = Assert.Single(fixture.Sms.Sent, m => m.Phone == "+966571111111");
        Assert.Contains("https://maps.google.com/?q=24.55,46.61", contactSms.Message);
        Assert.Contains("https://ata.sa/t/", contactSms.Message);
        Assert.DoesNotContain(fixture.Sms.Sent, m => m.Phone == "+966571111112");
        Assert.Contains(fixture.Sms.Sent, m => m.Phone == "+966511111111");
        var pushed = await opened.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(caseId.ToString(), pushed.GetProperty("id").GetString());
        Assert.Equal("critical", pushed.GetProperty("priority").GetString());

        var again = await passenger.Client.PostAsJsonAsync("/api/v1/safety/sos", new { tripId = ride.TripId, lat = 24.551m, lng = 46.611m, notifyTrustedContacts = true });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(caseId.ToString(), (await again.ReadJsonAsync()).GetProperty("caseId").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.SafetyCaseNotes.AnyAsync(n => n.CaseId == caseId && n.Kind == SafetyNoteKind.System && n.Body.Contains("ضغط متكرر"))));
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.SafetyCases.CountAsync(c => c.TripId == Guid.Parse(ride.TripId))));

        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.PostAsJsonAsync($"/api/v1/safety/sos/{caseId}/location", new { lat = 24.56m, lng = 46.62m })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ride.Driver.Client.PostAsJsonAsync($"/api/v1/safety/sos/{caseId}/location", new { lat = 24.56m, lng = 46.62m })).StatusCode);
        Assert.Equal(24.56m, (await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == caseId))).LastLat);
        var cancelled = await (await passenger.Client.PostAsJsonAsync($"/api/v1/safety/sos/{caseId}/cancel", new { reason = "accidental" })).ReadJsonAsync();
        Assert.Equal("high", cancelled.GetProperty("priority").GetString());
        Assert.Equal("open", cancelled.GetProperty("status").GetString());

        var driverSos = await (await ride.Driver.Client.PostAsJsonAsync("/api/v1/safety/sos", new { tripId = ride.TripId, lat = 24.55m, lng = 46.61m, notifyTrustedContacts = false })).ReadJsonAsync();
        Assert.Equal(SafetyCaseSource.DriverSos, (await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == Guid.Parse(driverSos.GetProperty("caseId").GetString()!)))).Source);

        var mine = await (await passenger.Client.GetAsync("/api/v1/safety/cases")).ReadJsonAsync();
        Assert.Equal(1, mine.GetProperty("total").GetInt32());
        admin.Dispose();
    }

    [Fact]
    public async Task Chat_is_open_between_assignment_and_trip_end_masks_numbers_and_is_limited_to_the_parties()
    {
        var area = TripFlow.Area(3);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var created = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString()!;
        var closed = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/messages", new { body = "مرحبا" });
        Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
        Assert.Equal("chat_closed", await closed.ErrorCodeAsync());

        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();

        await using var passengerHub = TripFlow.Hub(fixture, passenger.Token);
        var received = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        passengerHub.On<JsonElement>("TripMessage", m => received.TrySetResult(m));
        await passengerHub.StartAsync();

        var quick = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/messages", new { quickReplyCode = "arrived" });
        Assert.Equal(HttpStatusCode.Created, quick.StatusCode);
        var quickBody = await quick.ReadJsonAsync();
        Assert.Equal("quick_reply", quickBody.GetProperty("kind").GetString());
        Assert.Equal("وصلت إلى نقطة الالتقاط", quickBody.GetProperty("body").GetString());
        Assert.True(quickBody.GetProperty("isMine").GetBoolean());
        var pushed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("driver", pushed.GetProperty("senderRole").GetString());
        Assert.False(pushed.GetProperty("isMine").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/messages", new { quickReplyCode = "arrived" })).StatusCode);

        var text = await (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/messages", new { body = "اتصل بي على 0551234567 أو 055 123 4567 شكراً" })).ReadJsonAsync();
        Assert.Equal("اتصل بي على •••• أو •••• شكراً", text.GetProperty("body").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AnyAsync(d => d.UserId == driver.UserId && d.EventCode == NotificationTypes.TripMessage && d.Channel == NotificationChannel.Push)));

        var stranger = await SafetyFlow.PassengerAsync(fixture);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/messages", new { body = "hi" })).StatusCode);

        var messages = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/messages")).ReadJsonAsync();
        Assert.Equal(2, messages.GetArrayLength());
        Assert.False(messages[0].GetProperty("isMine").GetBoolean());
        Assert.True(messages[1].GetProperty("isMine").GetBoolean());
        var after = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/messages?after={messages[0].GetProperty("id").GetString()}")).ReadJsonAsync();
        Assert.Single(after.EnumerateArray());
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/messages/read", new { upToId = messages[1].GetProperty("id").GetString() })).StatusCode);
        var driverView = await (await driver.Client.GetAsync($"/api/v1/driver/trips/{tripId}/messages")).ReadJsonAsync();
        Assert.NotEqual(JsonValueKind.Null, driverView[0].GetProperty("readAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, driverView[1].GetProperty("readAt").ValueKind);

        var call = await (await passenger.Client.PostAsync($"/api/v1/passenger/trips/{tripId}/call", null)).ReadJsonAsync();
        Assert.Equal("unavailable", call.GetProperty("mode").GetString());
        Assert.False(call.GetProperty("available").GetBoolean());
        Assert.Equal(JsonValueKind.Null, call.GetProperty("proxyNumber").ValueKind);
        var replies = await (await passenger.Client.GetAsync("/api/v1/catalog/chat-quick-replies?role=passenger")).ReadJsonAsync();
        Assert.Equal(4, replies.GetArrayLength());

        using var admin = await fixture.LoginAdminAsync();
        var adminMessages = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/messages")).ReadJsonAsync();
        Assert.Equal(2, adminMessages.GetArrayLength());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "trip_messages.view" && a.EntityId == Guid.Parse(tripId))));

        var ride = new SafetyFlow.Ride(passenger, driver, Guid.Empty, tripId, created.GetProperty("tripNumber").GetString()!);
        await SafetyFlow.CompleteAsync(ride);
        var late = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/messages", new { body = "شكراً" });
        Assert.Equal("chat_closed", await late.ErrorCodeAsync());
        Assert.Equal(2, (await (await driver.Client.GetAsync($"/api/v1/driver/trips/{tripId}/messages")).ReadJsonAsync()).GetArrayLength());

        // Retention:TripMessagesDays (180): the share-expiry job purges older chat messages.
        await fixture.Factory.WithDbAsync(async db =>
        {
            db.TripMessages.Add(new TripMessage { TripId = Guid.Parse(tripId), SenderRole = TripMessageSender.System, Kind = TripMessageKind.System, Body = "old", CreatedAt = fixture.Factory.Clock.UtcNow.AddDays(-181) });
            return await db.SaveChangesAsync();
        });
        await fixture.Factory.WithServiceAsync<ATA.Api.Modules.Safety.SafetyMonitor, int>(m => m.RunShareExpiryAsync(CancellationToken.None));
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.TripMessages.CountAsync(m => m.TripId == Guid.Parse(tripId))));
    }

    [Fact]
    public async Task Monitor_raises_one_stop_alert_away_from_known_places_and_the_check_cycle_escalates_or_times_out()
    {
        await SafetyFlow.OnDutyAdminAsync(fixture);
        var adminUserId = await SafetyFlow.AdminUserIdAsync(fixture);
        var area = TripFlow.Area(4);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        await SafetyFlow.StartAsync(ride);
        var tripId = Guid.Parse(ride.TripId);
        var (stopLat, stopLng) = (area.Lat + 0.025m, area.Lng + 0.01m); // ≈1 km off the straight route, far from pickup / dropoff
        await SeedTrackAsync(ride.DriverId, tripId, TimeSpan.FromMinutes(6), _ => (stopLat, stopLng));

        Assert.Equal(JsonValueKind.Null, (await (await ride.Passenger.Client.GetAsync("/api/v1/safety/alerts/pending")).ReadJsonAsync()).ValueKind);
        await fixture.Factory.RunSafetyMonitorAsync();
        var alert = Assert.Single(await AlertsAsync(tripId));
        Assert.Equal(SafetyAlertType.UnexpectedStop, alert.Type);
        Assert.Equal(SafetyAlertStatus.PendingRider, alert.Status);
        Assert.Contains("stoppedSeconds", alert.Metrics);
        var check = await fixture.Factory.WithDbAsync(db => db.Notifications.FirstAsync(n => n.UserId == ride.Passenger.UserId && n.Type == NotificationTypes.SafetyCheck));
        var data = JsonDocument.Parse(check.Data!).RootElement;
        Assert.Equal(alert.Id.ToString(), data.GetProperty("alertId").GetString());
        Assert.Equal("unexpected_stop", data.GetProperty("alertType").GetString());
        Assert.Equal($"ata://safety/check/{alert.Id}", data.GetProperty("deepLink").GetString());

        await fixture.Factory.RunSafetyMonitorAsync();
        Assert.Single(await AlertsAsync(tripId)); // one open alert per (trip, type)

        var pending = await (await ride.Passenger.Client.GetAsync("/api/v1/safety/alerts/pending")).ReadJsonAsync();
        Assert.Equal(alert.Id.ToString(), pending.GetProperty("id").GetString());
        var help = await (await ride.Passenger.Client.PostAsJsonAsync($"/api/v1/safety/alerts/{alert.Id}/respond", new { response = "need_help" })).ReadJsonAsync();
        Assert.Equal("escalated", help.GetProperty("status").GetString());
        var escalated = (await AlertsAsync(tripId)).Single();
        Assert.NotNull(escalated.SafetyCaseId);
        var alertCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == escalated.SafetyCaseId));
        Assert.Equal(SafetyPriority.High, alertCase.Priority);
        Assert.Equal(SafetyCaseSource.Alert, alertCase.Source);
        Assert.Equal(SafetyCaseType.UnexpectedStop, alertCase.Type);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.AnyAsync(d => d.UserId == adminUserId && d.EventCode == NotificationTypes.SafetyAlert)));
        Assert.Equal(HttpStatusCode.Conflict, (await ride.Passenger.Client.PostAsJsonAsync($"/api/v1/safety/alerts/{alert.Id}/respond", new { response = "ok" })).StatusCode);

        // Cooldown: still stopped, but no new alert within 10 minutes of the closure.
        await SeedTrackAsync(ride.DriverId, tripId, TimeSpan.FromMinutes(1), _ => (stopLat, stopLng));
        await fixture.Factory.RunSafetyMonitorAsync();
        Assert.Single(await AlertsAsync(tripId), a => a.Type == SafetyAlertType.UnexpectedStop);
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(11));
        await SeedTrackAsync(ride.DriverId, tripId, TimeSpan.FromMinutes(6), _ => (stopLat, stopLng));
        await fixture.Factory.RunSafetyMonitorAsync();
        var second = (await AlertsAsync(tripId)).Where(a => a.Type == SafetyAlertType.UnexpectedStop).OrderBy(a => a.DetectedAt).Last();
        Assert.Equal(SafetyAlertStatus.PendingRider, second.Status);

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        await fixture.Factory.RunSafetyCheckTimeoutsAsync();
        var timedOut = await fixture.Factory.WithDbAsync(db => db.SafetyAlerts.FirstAsync(a => a.Id == second.Id));
        Assert.Equal(SafetyAlertStatus.NoResponse, timedOut.Status);
        var timeoutCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == timedOut.SafetyCaseId));
        Assert.Equal(SafetyReporterRole.System, timeoutCase.ReporterRole);
        Assert.Equal(SafetyPriority.High, timeoutCase.Priority);
    }

    [Fact]
    public async Task Monitor_ignores_a_stop_near_the_dropoff_and_detects_route_deviation_and_overrun()
    {
        var nearArea = TripFlow.Area(5);
        var near = await SafetyFlow.AssignedRideAsync(fixture, nearArea);
        await SafetyFlow.StartAsync(near);
        await SeedTrackAsync(near.DriverId, Guid.Parse(near.TripId), TimeSpan.FromMinutes(7), _ => (nearArea.Lat + 0.0499m, nearArea.Lng + 0.05m));
        await fixture.Factory.RunSafetyMonitorAsync();
        Assert.Empty(await AlertsAsync(Guid.Parse(near.TripId)));

        var area = TripFlow.Area(6);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        await SafetyFlow.StartAsync(ride);
        var tripId = Guid.Parse(ride.TripId);
        // ≈2.5 km east of the straight route for 3 minutes, moving (no stop).
        await SeedTrackAsync(ride.DriverId, tripId, TimeSpan.FromMinutes(3), i => (area.Lat + 0.01m + 0.0005m * i, area.Lng + 0.045m + 0.0005m * i), speed: 10m);
        await fixture.Factory.RunSafetyMonitorAsync();
        var deviation = Assert.Single(await AlertsAsync(tripId));
        Assert.Equal(SafetyAlertType.RouteDeviation, deviation.Type);
        var metrics = JsonDocument.Parse(deviation.Metrics!).RootElement;
        Assert.True(metrics.GetProperty("deviationMeters").GetInt32() > 2000);
        Assert.True(metrics.GetProperty("deviationSeconds").GetInt32() > 120);
        (await ride.Passenger.Client.PostAsJsonAsync($"/api/v1/safety/alerts/{deviation.Id}/respond", new { response = "ok" })).EnsureSuccessStatusCode();
        Assert.Equal(SafetyAlertStatus.ResolvedOk, (await AlertsAsync(tripId)).Single().Status);

        var trip = await fixture.Factory.WithDbAsync(db => db.Trips.FirstAsync(t => t.Id == tripId));
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(Math.Max(trip.EstimatedDurationS * 2, trip.EstimatedDurationS + 15 * 60) + 60));
        await fixture.Factory.RunSafetyMonitorAsync();
        var overrun = Assert.Single(await AlertsAsync(tripId), a => a.Type == SafetyAlertType.TripOverrun);
        var overrunMetrics = JsonDocument.Parse(overrun.Metrics!).RootElement;
        Assert.Equal(trip.EstimatedDurationS, overrunMetrics.GetProperty("estimatedSeconds").GetInt32());

        using var admin = await fixture.LoginAdminAsync();
        var alerts = await (await admin.GetAsync($"/api/v1/admin/safety/alerts?tripId={tripId}")).ReadJsonAsync();
        Assert.Equal(2, alerts.GetProperty("total").GetInt32());
        Assert.Equal(ride.TripNumber, alerts.GetProperty("items")[0].GetProperty("tripNumber").GetString());
        var dismissed = await (await admin.PostAsJsonAsync($"/api/v1/admin/safety/alerts/{overrun.Id}/dismiss", new { note = "الراكب بخير هاتفياً" })).ReadJsonAsync();
        Assert.Equal("dismissed", dismissed.GetProperty("status").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "safety_alert.dismiss" && a.EntityId == overrun.Id)));
    }

    [Fact]
    public async Task Case_workflow_stamps_first_response_once_audits_every_action_and_guards_trusted_contact_reads()
    {
        var area = TripFlow.Area(7);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        (await ride.Passenger.Client.PostAsJsonAsync("/api/v1/safety/trusted-contacts", new { name = "أخي", phoneNumber = "0572222222" })).EnsureSuccessStatusCode();
        using var admin = await fixture.LoginAdminAsync();
        var forbidden = await admin.GetAsync($"/api/v1/admin/users/{ride.Passenger.UserId}/trusted-contacts");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var sos = await (await ride.Passenger.Client.PostAsJsonAsync("/api/v1/safety/sos", new { tripId = ride.TripId, lat = area.Lat, lng = area.Lng, notifyTrustedContacts = false })).ReadJsonAsync();
        var caseId = sos.GetProperty("caseId").GetString()!;
        var list = await (await admin.GetAsync($"/api/v1/admin/safety/cases?search={ride.TripNumber}")).ReadJsonAsync();
        var row = Assert.Single(list.GetProperty("items").EnumerateArray());
        Assert.Equal(caseId, row.GetProperty("id").GetString());
        Assert.Equal(ride.TripId, row.GetProperty("tripId").GetString());
        Assert.Equal("سارة أحمد", row.GetProperty("reporterName").GetString());
        var summary = await (await admin.GetAsync("/api/v1/admin/safety/summary")).ReadJsonAsync();
        Assert.True(summary.GetProperty("open").GetProperty("critical").GetInt32() >= 1);

        var detail = await (await admin.GetAsync($"/api/v1/admin/safety/cases/{caseId}")).ReadJsonAsync();
        Assert.StartsWith("+9665", detail.GetProperty("trip").GetProperty("passenger").GetProperty("phoneNumber").GetString());
        Assert.DoesNotContain("*", detail.GetProperty("trip").GetProperty("driver").GetProperty("phoneNumber").GetString());
        Assert.Equal("driver", detail.GetProperty("liveLocation").GetProperty("source").GetString());
        Assert.Equal("system", detail.GetProperty("notes")[0].GetProperty("kind").GetString());

        var contacts = await (await admin.GetAsync($"/api/v1/admin/users/{ride.Passenger.UserId}/trusted-contacts")).ReadJsonAsync();
        Assert.Single(contacts.EnumerateArray());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "trusted_contacts.view" && a.EntityId == ride.Passenger.UserId)));

        var assigned = await (await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/assign", new { userId = (Guid?)null })).ReadJsonAsync();
        Assert.Equal("in_progress", assigned.GetProperty("status").GetString());
        Assert.Equal("ATA Admin", assigned.GetProperty("assignedToName").GetString());
        var firstResponse = assigned.GetProperty("firstResponseAt").GetString();
        Assert.NotNull(firstResponse);

        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(2));
        var noted = await (await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/notes", new { body = "تواصلنا مع الراكبة وهي بخير", kind = "contact_attempt", isInternal = false })).ReadJsonAsync();
        Assert.Equal(firstResponse, noted.GetProperty("firstResponseAt").GetString());
        Assert.Contains(noted.GetProperty("notes").EnumerateArray(), n => n.GetProperty("kind").GetString() == "contact_attempt" && n.GetProperty("authorName").GetString() == "ATA Admin");
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Passenger.UserId && n.Type == NotificationTypes.SafetyCaseUpdate)));
        var userCase = await (await ride.Passenger.Client.GetAsync($"/api/v1/safety/cases/{caseId}")).ReadJsonAsync();
        Assert.Single(userCase.GetProperty("publicNotes").EnumerateArray());

        var noTarget = await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/status", new { status = "escalated" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noTarget.StatusCode);
        var escalated = await (await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/status", new { status = "escalated", escalatedTo = "police", note = "بلاغ للشرطة" })).ReadJsonAsync();
        Assert.Equal("escalated", escalated.GetProperty("status").GetString());
        Assert.Equal("police", escalated.GetProperty("escalatedTo").GetString());
        var resolved = await (await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/resolve", new { resolutionCode = "resolved_contacted", resolution = "تم التواصل والتأكد" })).ReadJsonAsync();
        Assert.Equal("resolved", resolved.GetProperty("status").GetString());
        Assert.Equal(firstResponse, resolved.GetProperty("firstResponseAt").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/safety/cases/{caseId}/resolve", new { resolutionCode = "other", resolution = "x" })).StatusCode);

        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityId == Guid.Parse(caseId)).OrderBy(a => a.CreatedAt).Select(a => a.Action).ToListAsync());
        Assert.Equal(new[] { "safety_case.assign", "safety_case.note", "safety_case.resolve", "safety_case.status" }, audits.Order().ToArray());

        var manual = await admin.PostAsJsonAsync("/api/v1/admin/safety/cases", new { tripId = ride.TripId, type = "safety_report", priority = "medium", description = "اتصال هاتفي", subjectUserId = ride.Driver.UserId });
        Assert.Equal(HttpStatusCode.Created, manual.StatusCode);
        var manualBody = await manual.ReadJsonAsync();
        Assert.Equal("admin", manualBody.GetProperty("source").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "safety_case.create" && a.EntityId == Guid.Parse(manualBody.GetProperty("id").GetString()!))));
        Assert.Equal(HttpStatusCode.Forbidden, (await ride.Passenger.Client.GetAsync("/api/v1/admin/safety/cases")).StatusCode);
    }

    [Fact]
    public async Task Safety_report_and_lost_item_flows_respect_their_windows()
    {
        var area = TripFlow.Area(8);
        var ride = await SafetyFlow.AssignedRideAsync(fixture, area);
        var notCompleted = await ride.Passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/lost-items", new { itemCategory = "phone", description = "جوال" });
        Assert.Equal(HttpStatusCode.Conflict, notCompleted.StatusCode);
        await SafetyFlow.CompleteAsync(ride);

        var report = await ride.Passenger.Client.PostAsJsonAsync("/api/v1/safety/reports", new { tripId = ride.TripId, category = "harassment", description = "كلام غير لائق" });
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);
        var reportBody = await report.ReadJsonAsync();
        Assert.Equal("high", reportBody.GetProperty("priority").GetString());
        Assert.Equal("safety_report", reportBody.GetProperty("type").GetString());
        var reportCase = await fixture.Factory.WithDbAsync(db => db.SafetyCases.FirstAsync(c => c.Id == Guid.Parse(reportBody.GetProperty("id").GetString()!)));
        Assert.Equal(ride.Driver.UserId, reportCase.SubjectUserId);

        var lost = await ride.Passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/lost-items", new { itemCategory = "phone", description = "جوال أسود في المقعد الخلفي" });
        Assert.Equal(HttpStatusCode.Created, lost.StatusCode);
        var lostBody = await lost.ReadJsonAsync();
        var reportId = lostBody.GetProperty("id").GetString()!;
        Assert.Matches(@"^LI-\d{8}-\d{4}$", lostBody.GetProperty("reportNumber").GetString());
        Assert.Equal(ride.Passenger.Phone, lostBody.GetProperty("contactPhone").GetString());
        Assert.Equal("open", lostBody.GetProperty("status").GetString());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Driver.UserId && n.Type == "lost_item.reported")));

        var driverList = await (await ride.Driver.Client.GetAsync("/api/v1/driver/lost-items?status=open")).ReadJsonAsync();
        var driverItem = Assert.Single(driverList.GetProperty("items").EnumerateArray());
        Assert.False(driverItem.TryGetProperty("contactPhone", out _));
        var answered = await (await ride.Driver.Client.PostAsJsonAsync($"/api/v1/driver/lost-items/{reportId}/respond", new { found = true, note = "موجود معي" })).ReadJsonAsync();
        Assert.Equal("found", answered.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await ride.Driver.Client.PostAsJsonAsync($"/api/v1/driver/lost-items/{reportId}/respond", new { found = false })).StatusCode);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == ride.Passenger.UserId && n.Type == "lost_item.update")));

        using var admin = await fixture.LoginAdminAsync();
        var adminList = await (await admin.GetAsync($"/api/v1/admin/lost-items?search={ride.TripNumber}")).ReadJsonAsync();
        var adminItem = Assert.Single(adminList.GetProperty("items").EnumerateArray());
        Assert.Equal("سارة أحمد", adminItem.GetProperty("reporterName").GetString());
        Assert.Equal(ride.Passenger.Phone, adminItem.GetProperty("contactPhone").GetString());
        var returned = await (await admin.PatchAsJsonAsync($"/api/v1/admin/lost-items/{reportId}", new { status = "returned", note = "سُلّم للراكبة" })).ReadJsonAsync();
        Assert.Equal("returned", returned.GetProperty("status").GetString());
        var closed = await (await admin.PatchAsJsonAsync($"/api/v1/admin/lost-items/{reportId}", new { status = "closed" })).ReadJsonAsync();
        Assert.NotEqual(JsonValueKind.Null, closed.GetProperty("closedAt").ValueKind);
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.AuditLogs.CountAsync(a => a.Action == "lost_item.update" && a.EntityId == Guid.Parse(reportId))));
        var mine = await (await ride.Passenger.Client.GetAsync("/api/v1/passenger/lost-items")).ReadJsonAsync();
        Assert.Equal("closed", mine.GetProperty("items")[0].GetProperty("status").GetString());

        fixture.Factory.Clock.Advance(TimeSpan.FromDays(8));
        var (again, _) = await fixture.LoginAsync("passenger", ride.Passenger.Phone); // the old access token expired meanwhile
        var late = await again.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/lost-items", new { itemCategory = "bag", description = "حقيبة" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, late.StatusCode);
        Assert.Equal("lost_item_window_closed", await late.ErrorCodeAsync());
        var lateReport = await again.PostAsJsonAsync("/api/v1/safety/reports", new { tripId = ride.TripId, category = "unsafe_driving", description = "سرعة" });
        Assert.Equal("window_closed", (await lateReport.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("tripId").GetString());
    }

    private async Task<List<SafetyAlert>> AlertsAsync(Guid tripId) =>
        await fixture.Factory.WithDbAsync(db => db.SafetyAlerts.AsNoTracking().Where(a => a.TripId == tripId).ToListAsync());

    /// <summary>Writes one history point every 30 s over <paramref name="duration"/> ending now and moves the live location to the last point.</summary>
    private async Task SeedTrackAsync(Guid driverId, Guid tripId, TimeSpan duration, Func<int, (decimal Lat, decimal Lng)> at, decimal speed = 0m)
    {
        var now = fixture.Factory.Clock.UtcNow;
        var steps = (int)(duration.TotalSeconds / 30);
        await fixture.Factory.WithDbAsync(async db =>
        {
            for (var i = 0; i <= steps; i++)
            {
                var (lat, lng) = at(i);
                db.DriverLocationHistory.Add(new DriverLocationHistory { DriverId = driverId, TripId = tripId, Lat = lat, Lng = lng, RecordedAt = now - duration + TimeSpan.FromSeconds(30 * i) });
            }

            var location = await db.DriverLocations.FirstAsync(l => l.DriverId == driverId);
            (location.Lat, location.Lng) = at(steps);
            location.Speed = speed;
            location.UpdatedAt = now;
            await db.SaveChangesAsync();
            return true;
        });
    }

    private async Task<string> AdminTokenAsync()
    {
        using var anonymous = fixture.CreateClient();
        var auth = await (await anonymous.PostAsJsonAsync("/api/v1/auth/admin/login", new { username = "admin", password = "Admin@12345" })).ReadJsonAsync();
        return auth.GetProperty("accessToken").GetString()!;
    }
}
