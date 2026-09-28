using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Infrastructure.Push;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATA.Tests.Integration;

/// <summary>Push sender whose next results can be scripted; records every message.</summary>
public sealed class ScriptedPushSender : IPushSender
{
    public ConcurrentQueue<Func<PushMessage, PushSendResult>> Next { get; } = new();
    public ConcurrentBag<PushMessage> Sent { get; } = [];

    public string Provider => "scripted";

    public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken ct)
    {
        Sent.Add(message);
        return Task.FromResult(Next.TryDequeue(out var result)
            ? result(message)
            : new PushSendResult(true, $"msg-{Guid.NewGuid():N}", message.ExternalUserIds.Count, [], null, null, false));
    }
}

public sealed class NotificationFixture() : ApiFixture(null, services =>
{
    services.RemoveAll<IPushSender>();
    services.AddSingleton<ScriptedPushSender>();
    services.AddSingleton<IPushSender>(sp => sp.GetRequiredService<ScriptedPushSender>());
})
{
    public ScriptedPushSender Push => Factory.Services.GetRequiredService<ScriptedPushSender>();

    public async Task<DispatchResult> DispatchAsync(NotificationRequest request)
    {
        using var scope = Factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().DispatchAsync(request, CancellationToken.None);
        await scope.ServiceProvider.GetRequiredService<AtaDbContext>().SaveChangesAsync();
        return result;
    }
}

public class NotificationTests(NotificationFixture fixture) : IClassFixture<NotificationFixture>
{
    [Fact]
    public async Task Preference_off_skips_push_but_critical_and_safety_events_bypass_it()
    {
        var (passenger, auth) = await fixture.LoginAsync("passenger");
        var userId = PaymentFlow.UserId(auth);
        (await passenger.PutAsJsonAsync("/api/v1/me/notification-preferences", new { trips = false, wallet = true, safety = false, offers = true })).EnsureSuccessStatusCode();

        var completed = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.TripCompleted, userId,
            NotificationPlaceholders.Of(("tripNumber", "T-1")).Money("fare", 46m), "trip", Guid.NewGuid()));
        Assert.NotNull(completed.Notification);
        var completedPush = Assert.Single(completed.Deliveries);
        Assert.Equal(DeliveryStatus.Skipped, completedPush.Status);
        Assert.Equal(DeliverySkipReasons.PreferenceOff, completedPush.SkippedReason);

        var assigned = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.TripDriverAssigned, userId,
            NotificationPlaceholders.Of(("driverName", "محمد"), ("vehicle", "Camry"), ("plateNumber", "ABC 1"), ("etaMinutes", 4)), "trip", Guid.NewGuid()));
        Assert.Equal(DeliveryStatus.Queued, Assert.Single(assigned.Deliveries).Status);

        var safety = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyCheck, userId, NotificationPlaceholders.Of(("alertType", "توقف طويل")), "alert", Guid.NewGuid()));
        var safetyPush = Assert.Single(safety.Deliveries);
        Assert.Equal(DeliveryStatus.Queued, safetyPush.Status);
        var payload = JsonDocument.Parse(safetyPush.Payload).RootElement;
        Assert.Equal(2, payload.GetProperty("buttons").GetArrayLength());
        Assert.Equal("safety", payload.GetProperty("category").GetString());

        var inbox = await (await passenger.GetAsync("/api/v1/notifications?category=trips")).ReadJsonAsync();
        Assert.Equal(2, inbox.GetProperty("total").GetInt32());
        var item = inbox.GetProperty("items").EnumerateArray().First(i => i.GetProperty("type").GetString() == "trip.completed");
        Assert.Equal("trips", item.GetProperty("category").GetString());
        Assert.StartsWith("ata://rides/", item.GetProperty("data").GetProperty("deepLink").GetString());
        Assert.EndsWith("/receipt", item.GetProperty("data").GetProperty("deepLink").GetString());
        Assert.Equal(3, (await (await passenger.GetAsync("/api/v1/notifications/unread-count")).ReadJsonAsync()).GetProperty("unreadCount").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.PostAsync($"/api/v1/notifications/{item.GetProperty("id").GetString()}/opened", null)).StatusCode);
        Assert.Equal(2, (await (await passenger.GetAsync("/api/v1/notifications/unread-count")).ReadJsonAsync()).GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task Templates_render_ar_and_en_placeholders_inactive_templates_turn_channels_off_and_defaults_apply()
    {
        var (_, auth) = await fixture.LoginAsync("passenger");
        var userId = PaymentFlow.UserId(auth);
        var result = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.TripCompleted, userId,
            NotificationPlaceholders.Of(("tripNumber", "T-20260928-00042")).Money("fare", 46m), "trip", Guid.NewGuid()));
        Assert.Equal("وصلت بسلامة. أجرة الرحلة T-20260928-00042: 46.00 ر.س", result.Notification!.BodyAr);
        Assert.Equal("You have arrived. Trip T-20260928-00042 fare: SAR 46.00", result.Notification.BodyEn);
        var push = JsonDocument.Parse(result.Deliveries[0].Payload).RootElement;
        Assert.Equal("Trip completed", push.GetProperty("headings").GetProperty("en").GetString());
        Assert.Equal("trip.completed", push.GetProperty("data").GetProperty("eventCode").GetString());

        using var admin = await fixture.LoginAdminAsync();
        var templates = await (await admin.GetAsync("/api/v1/admin/notification-templates?code=trip.started")).ReadJsonAsync();
        var started = Assert.Single(templates.EnumerateArray());
        var id = started.GetProperty("id").GetString();
        var off = await admin.PutAsJsonAsync($"/api/v1/admin/notification-templates/{id}", new
        {
            titleAr = "بدأت", titleEn = "Started", bodyAr = "إلى {dropoffName}", bodyEn = "To {dropoffName}", isActive = false,
        });
        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        var silent = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.TripStarted, userId, NotificationPlaceholders.Of(("dropoffName", "العمل"))));
        Assert.Null(silent.Notification);
        Assert.Empty(silent.Deliveries);

        var preview = await (await admin.PostAsJsonAsync($"/api/v1/admin/notification-templates/{id}/preview", new { placeholders = new { dropoffName = "المطار" }, language = "ar" })).ReadJsonAsync();
        Assert.Equal("إلى المطار", preview.GetProperty("body").GetString());
        Assert.Equal(1, preview.GetProperty("smsSegments").GetInt32());

        // No template row at all → the code default text is used.
        await fixture.Factory.WithDbAsync(db => db.NotificationTemplates.Where(t => t.Code == "promo.new").ExecuteDeleteAsync());
        fixture.Factory.Services.GetRequiredService<NotificationTemplateCache>().Invalidate();
        var promo = await fixture.DispatchAsync(new NotificationRequest("promo.new", userId, NotificationPlaceholders.Of(("code", "ATA10"), ("title", "خصم 10%"))));
        Assert.Equal("خصم 10% · استخدم الكود ATA10", promo.Notification!.BodyAr);

        var unknownCode = await admin.PostAsJsonAsync("/api/v1/admin/notification-templates", new { code = "trip.teleported", channel = "push", titleAr = "x", titleEn = "x", bodyAr = "x", bodyEn = "x", isActive = true });
        Assert.Equal("unknown_event_code", await unknownCode.ErrorCodeAsync());
        var badPlaceholder = await admin.PostAsJsonAsync("/api/v1/admin/notification-templates", new { code = "trip.no_drivers", channel = "sms", bodyAr = "لا يوجد {driverName}", bodyEn = "None {categoryName}", isActive = true });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badPlaceholder.StatusCode);
        var error = (await badPlaceholder.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("template_placeholder_invalid", error.GetProperty("code").GetString());
        Assert.Equal("driverName", error.GetProperty("details").GetProperty("unknownPlaceholders")[0].GetString());
        var created = await admin.PostAsJsonAsync("/api/v1/admin/notification-templates", new { code = "trip.no_drivers", channel = "sms", bodyAr = "لا يوجد كابتن {categoryName}", bodyEn = "No {categoryName} driver", isActive = true });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var events = await (await admin.GetAsync("/api/v1/admin/notification-events")).ReadJsonAsync();
        Assert.Contains(events.EnumerateArray(), e => e.GetProperty("code").GetString() == "safety.sos_contact" && e.GetProperty("isCritical").GetBoolean());
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Select(a => a.Action).ToListAsync());
        Assert.Contains("notification_template.update", audits);
        Assert.Contains("notification_template.create", audits);
    }

    [Fact]
    public async Task Delivery_worker_sends_retries_transient_failures_with_backoff_and_skips_users_without_subscription()
    {
        var (_, auth) = await fixture.LoginAsync("passenger");
        var userId = PaymentFlow.UserId(auth);
        await fixture.Factory.RunNotificationWorkerAsync(); // drain deliveries queued by other tests
        fixture.Push.Next.Clear();
        var result = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.TripDriverArrived, userId,
            NotificationPlaceholders.Of(("driverName", "سالم"), ("plateNumber", "XYZ 9"), ("freeWaitingMinutes", 3)), "trip", Guid.NewGuid()));
        var deliveryId = result.Deliveries.Single().Id;
        fixture.Push.Next.Enqueue(_ => PushSendResult.Transient("http_500", "boom"));
        await fixture.Factory.RunNotificationWorkerAsync();
        var failed = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal(DeliveryStatus.Failed, failed.Status);
        Assert.Equal(1, failed.Attempts);
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddSeconds(30), failed.NextAttemptAt);

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(31));
        await fixture.Factory.RunNotificationWorkerAsync();
        var sent = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.FirstAsync(d => d.Id == deliveryId));
        Assert.Equal(DeliveryStatus.Sent, sent.Status);
        Assert.Equal("scripted", sent.Provider);
        Assert.NotNull(sent.ProviderMessageId);
        var message = fixture.Push.Sent.Last(m => m.IdempotencyKey == deliveryId.ToString());
        Assert.Equal([userId], message.ExternalUserIds);
        Assert.Equal("trip-" + result.Notification!.Data!.Split("entityId\":\"")[1][..36], message.CollapseId);

        fixture.Push.Next.Enqueue(m => new PushSendResult(true, null, 0, m.ExternalUserIds, null, null, false));
        var noSubscription = await fixture.DispatchAsync(new NotificationRequest(NotificationTypes.PaymentFailed, userId, NotificationPlaceholders.Of(("reason", "declined")).Money("amount", 10m)));
        await fixture.Factory.RunNotificationWorkerAsync();
        var skipped = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.FirstAsync(d => d.Id == noSubscription.Deliveries[0].Id));
        Assert.Equal(DeliveryStatus.Skipped, skipped.Status);
        Assert.Equal(DeliverySkipReasons.NoSubscription, skipped.SkippedReason);

        using var admin = await fixture.LoginAdminAsync();
        var log = await (await admin.GetAsync($"/api/v1/admin/notification-deliveries?userId={userId}&channel=push")).ReadJsonAsync();
        Assert.True(log.GetProperty("total").GetInt32() >= 2);
        Assert.Equal(HttpStatusCode.Accepted, (await admin.PostAsync($"/api/v1/admin/notification-deliveries/{skipped.Id}/retry", null)).StatusCode);
        await fixture.Factory.RunNotificationWorkerAsync();
        Assert.Equal(DeliveryStatus.Sent, (await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.FirstAsync(d => d.Id == skipped.Id))).Status);
    }

    [Fact]
    public async Task Campaign_audience_selects_by_role_and_language_respects_offers_preference_and_tracks_stats()
    {
        var (english, englishAuth) = await fixture.LoginAsync("passenger");
        (await english.PatchAsJsonAsync("/api/v1/me", new { language = "en" })).EnsureSuccessStatusCode();
        var (optedOut, optedOutAuth) = await fixture.LoginAsync("passenger");
        (await optedOut.PatchAsJsonAsync("/api/v1/me", new { language = "en" })).EnsureSuccessStatusCode();
        (await optedOut.PutAsJsonAsync("/api/v1/me/notification-preferences", new { trips = true, wallet = true, safety = true, offers = false })).EnsureSuccessStatusCode();
        var (driver, _) = await fixture.LoginAsync("driver");
        (await driver.PatchAsJsonAsync("/api/v1/me", new { language = "en" })).EnsureSuccessStatusCode();

        using var admin = await fixture.LoginAdminAsync();
        var audience = new { roles = new[] { "passenger" }, languages = new[] { "en" } };
        var preview = await (await admin.PostAsJsonAsync("/api/v1/admin/notification-campaigns/audience-preview", new { audience })).ReadJsonAsync();
        Assert.Equal(2, preview.GetProperty("count").GetInt32());
        Assert.All(preview.GetProperty("sample").EnumerateArray(), s => Assert.Contains("*", s.GetProperty("phoneMasked").GetString()));

        var created = await admin.PostAsJsonAsync("/api/v1/admin/notification-campaigns", new
        {
            name = "Weekend promo", category = "promotions", channels = new[] { "inapp", "push" }, audience,
            titleAr = "عرض نهاية الأسبوع", titleEn = "Weekend offer", bodyAr = "خصم 20%", bodyEn = "20% off", deepLink = "ata://promotions",
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var campaignId = (await created.ReadJsonAsync()).GetProperty("id").GetString();
        Assert.Equal("scheduled", (await (await admin.PostAsync($"/api/v1/admin/notification-campaigns/{campaignId}/send-now", null)).ReadJsonAsync()).GetProperty("status").GetString());
        var notEditable = await admin.PutAsJsonAsync($"/api/v1/admin/notification-campaigns/{campaignId}", new
        {
            name = "x", category = "promotions", channels = new[] { "push" }, audience, titleAr = "x", titleEn = "x", bodyAr = "x", bodyEn = "x",
        });
        Assert.Equal("campaign_not_editable", await notEditable.ErrorCodeAsync());

        await fixture.Factory.WithServiceAsync<CampaignService, int>(s => s.RunDueAsync(CancellationToken.None));
        var campaign = await (await admin.GetAsync($"/api/v1/admin/notification-campaigns/{campaignId}")).ReadJsonAsync();
        Assert.Equal("sent", campaign.GetProperty("status").GetString());
        Assert.Equal(2, campaign.GetProperty("targetCount").GetInt32());
        Assert.Equal(2, campaign.GetProperty("inappCreated").GetInt32());
        Assert.Equal(1, campaign.GetProperty("pushSent").GetInt32());
        Assert.Equal(1, campaign.GetProperty("pushSkipped").GetInt32());
        var pushed = fixture.Push.Sent.Single(m => m.IdempotencyKey.StartsWith($"campaign:{campaignId}"));
        Assert.Equal([PaymentFlow.UserId(englishAuth)], pushed.ExternalUserIds);
        var skipped = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.SingleAsync(d => d.CampaignId == Guid.Parse(campaignId!) && d.UserId == PaymentFlow.UserId(optedOutAuth)));
        Assert.Equal(DeliverySkipReasons.PreferenceOff, skipped.SkippedReason);

        var inbox = await (await english.GetAsync("/api/v1/notifications?category=promotions")).ReadJsonAsync();
        var item = Assert.Single(inbox.GetProperty("items").EnumerateArray());
        Assert.Equal("عرض نهاية الأسبوع", item.GetProperty("title").GetString());
        await english.PostAsync($"/api/v1/notifications/{item.GetProperty("id").GetString()}/opened", null);
        Assert.Equal(1, (await (await admin.GetAsync($"/api/v1/admin/notification-campaigns/{campaignId}")).ReadJsonAsync()).GetProperty("openedCount").GetInt32());

        var draft = await (await admin.PostAsJsonAsync("/api/v1/admin/notification-campaigns", new
        {
            name = "Later", category = "system", channels = new[] { "inapp" }, audience = new { userIds = new[] { PaymentFlow.UserId(englishAuth) } },
            titleAr = "تنبيه", titleEn = "Notice", bodyAr = "نص", bodyEn = "Text",
        })).ReadJsonAsync();
        var draftId = draft.GetProperty("id").GetString();
        (await admin.PostAsJsonAsync($"/api/v1/admin/notification-campaigns/{draftId}/schedule", new { scheduledAt = "2026-10-05T09:00:00Z" })).EnsureSuccessStatusCode();
        Assert.Equal("cancelled", (await (await admin.PostAsync($"/api/v1/admin/notification-campaigns/{draftId}/cancel", null)).ReadJsonAsync()).GetProperty("status").GetString());
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "notification_campaign").Select(a => a.Action).ToListAsync());
        Assert.Contains("notification_campaign.send", audits);
        Assert.Contains("notification_campaign.cancel", audits);
    }

    [Fact]
    public async Task Document_expiry_scanner_notifies_once_per_offset_then_expires_and_takes_the_driver_offline()
    {
        var area = TripFlow.Area(0);
        var (driver, auth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن المستندات");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);
        var userId = PaymentFlow.UserId(auth);
        var today = DateOnly.FromDateTime(fixture.Factory.Clock.UtcNow.AddHours(3));
        var documentId = await fixture.Factory.WithDbAsync(async db =>
        {
            var doc = await db.DriverDocuments.FirstAsync(d => d.DriverId == driverId && d.DocumentTypeId == SeedIds.DocumentTypes.DrivingLicense);
            doc.ExpiresAt = today.AddDays(30);
            await db.SaveChangesAsync();
            return doc.Id;
        });

        await fixture.Factory.WithServiceAsync<DocumentExpiryScanner, int>(s => s.RunOnceAsync(CancellationToken.None));
        await fixture.Factory.WithServiceAsync<DocumentExpiryScanner, int>(s => s.RunOnceAsync(CancellationToken.None));
        var expiring = await fixture.Factory.WithDbAsync(db => db.Notifications.Where(n => n.UserId == userId && n.Type == NotificationTypes.DocumentExpiring).ToListAsync());
        var notice = Assert.Single(expiring);
        Assert.Contains("30", notice.BodyAr);
        Assert.Equal([30], await fixture.Factory.WithDbAsync(db => db.DocumentExpiryNotices.Where(n => n.DriverDocumentId == documentId).Select(n => n.OffsetDays).ToListAsync()));

        await fixture.Factory.WithDbAsync(async db =>
        {
            var doc = await db.DriverDocuments.FirstAsync(d => d.Id == documentId);
            doc.ExpiresAt = today.AddDays(1);
            return await db.SaveChangesAsync();
        });
        await fixture.Factory.WithServiceAsync<DocumentExpiryScanner, int>(s => s.RunOnceAsync(CancellationToken.None));
        var oneDay = await fixture.Factory.WithDbAsync(db => db.NotificationDeliveries.Where(d => d.UserId == userId && d.EventCode == NotificationTypes.DocumentExpiring).Select(d => d.Channel).ToListAsync());
        Assert.Contains(NotificationChannel.Sms, oneDay);

        await fixture.Factory.WithDbAsync(async db =>
        {
            var doc = await db.DriverDocuments.FirstAsync(d => d.Id == documentId);
            doc.ExpiresAt = today.AddDays(-1);
            return await db.SaveChangesAsync();
        });
        await fixture.Factory.WithServiceAsync<DocumentExpiryScanner, int>(s => s.RunOnceAsync(CancellationToken.None));
        var document = await fixture.Factory.WithDbAsync(db => db.DriverDocuments.FirstAsync(d => d.Id == documentId));
        Assert.Equal(DocumentStatus.Expired, document.Status);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.IsOnline).FirstAsync()));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == userId && n.Type == NotificationTypes.DocumentExpired)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "document.expire" && a.EntityId == documentId)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.DriverStatusLogs.AnyAsync(l => l.DriverId == driverId && !l.IsOnline)));
    }

    [Fact]
    public async Task Admin_duty_flag_is_stored_on_the_admin_account()
    {
        using var admin = await fixture.LoginAdminAsync();
        Assert.False((await (await admin.GetAsync("/api/v1/admin/me/duty")).ReadJsonAsync()).GetProperty("onDuty").GetBoolean());
        Assert.True((await (await admin.PutAsJsonAsync("/api/v1/admin/me/duty", new { onDuty = true })).ReadJsonAsync()).GetProperty("onDuty").GetBoolean());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AdminAccounts.AnyAsync(a => a.Username == "admin" && a.OnDuty)));
    }
}
