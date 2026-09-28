using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Admin;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Notifications;

/// <summary>
/// <c>DocumentExpiryScanJob</c> (doc 08 §F13.8), on Riyadh dates. For every verified document with an expiry date: once the days left reach an
/// offset of <c>Notifications:DocumentExpiryOffsetsDays</c> (30/7/1) → <c>document.expiring</c> once per offset (<c>document_expiry_notices</c>;
/// SMS on the last offset); once expired → status <c>expired</c>, <c>document.expired</c>, and a required document takes an online driver without a
/// trip offline (status log + <c>document.expire</c> audit by the system).
/// </summary>
public sealed class DocumentExpiryScanner(AtaDbContext db, INotificationDispatcher notifications, IClock clock, IOptions<NotificationsOptions> options)
{
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var today = Formats.RiyadhDate(now);
        var offsets = options.Value.DocumentExpiryOffsetsDays.Where(o => o > 0).Distinct().OrderBy(o => o).ToArray();
        var horizon = today.AddDays(offsets.Length == 0 ? 0 : offsets.Max());
        var documents = await (from d in db.DriverDocuments
                               join t in db.DocumentTypes on d.DocumentTypeId equals t.Id
                               join dr in db.Drivers on d.DriverId equals dr.Id
                               where d.Status == DocumentStatus.Verified && d.ExpiresAt != null && d.ExpiresAt <= horizon
                               select new { Document = d, Type = t, Driver = dr }).ToListAsync(ct);
        var ids = documents.Select(x => x.Document.Id).ToList();
        var sent = (await db.DocumentExpiryNotices.AsNoTracking().Where(n => ids.Contains(n.DriverDocumentId)).Select(n => new { n.DriverDocumentId, n.OffsetDays }).ToListAsync(ct))
            .Select(n => (n.DriverDocumentId, n.OffsetDays)).ToHashSet();
        var notified = 0;
        foreach (var x in documents)
        {
            var expiresAt = x.Document.ExpiresAt!.Value;
            var daysLeft = expiresAt.DayNumber - today.DayNumber;
            var values = NotificationPlaceholders.Of(("daysLeft", daysLeft), ("expiresAt", expiresAt.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture)))
                .Localized("documentName", x.Type.NameAr, x.Type.NameEn);
            if (expiresAt < today)
            {
                if (sent.Contains((x.Document.Id, 0))) continue;
                x.Document.Status = DocumentStatus.Expired;
                db.DocumentExpiryNotices.Add(new DocumentExpiryNotice { DriverDocumentId = x.Document.Id, OffsetDays = 0, SentAt = now });
                await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.DocumentExpired, x.Driver.UserId, values, "document", x.Document.Id), ct);
                if (x.Type.IsRequired && x.Driver.IsOnline && x.Driver.CurrentTripId is null)
                {
                    x.Driver.IsOnline = false;
                    x.Driver.LastOnlineAt = now;
                    db.DriverStatusLogs.Add(new DriverStatusLog { DriverId = x.Driver.Id, IsOnline = false, ChangedAt = now });
                    await db.DriverLocations.Where(l => l.DriverId == x.Driver.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.IsOnline, false), ct);
                }

                db.AuditLogs.Add(new AuditLog
                {
                    ActorRole = "system", Action = "document.expire", EntityType = "driver_document", EntityId = x.Document.Id,
                    AfterJson = System.Text.Json.JsonSerializer.Serialize(new { status = "expired", driverId = x.Driver.Id, wentOffline = !x.Driver.IsOnline }, JsonDefaults.Options),
                });
                notified++;
                continue;
            }

            // The smallest offset that has been reached; each offset is notified once even if the job missed its exact day.
            var offset = offsets.Where(o => daysLeft <= o).DefaultIfEmpty(-1).Min();
            if (offset < 0 || sent.Contains((x.Document.Id, offset))) continue;
            db.DocumentExpiryNotices.Add(new DocumentExpiryNotice { DriverDocumentId = x.Document.Id, OffsetDays = offset, SentAt = now });
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.DocumentExpiring, x.Driver.UserId, values, "document", x.Document.Id,
                ExtraChannels: offset == offsets.Min() ? [NotificationChannel.Sms] : null), ct);
            notified++;
        }

        await db.SaveChangesAsync(ct);
        return notified;
    }
}

/// <summary>Runs <see cref="CampaignService.RunDueAsync"/> every <c>CampaignPollSeconds</c> and the document scan daily at <c>DocumentScanHourLocal</c>.</summary>
public sealed class NotificationJobsBackgroundService(IServiceScopeFactory scopes, IDistributedLock locks, IClock clock, IOptions<NotificationsOptions> options, ILogger<NotificationJobsBackgroundService> logger) : BackgroundService
{
    private DateOnly? _lastScan;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, settings.CampaignPollSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunLockedAsync("campaign_sender", sp => sp.GetRequiredService<CampaignService>().RunDueAsync(stoppingToken), stoppingToken);
                var local = Formats.ToRiyadh(clock.UtcNow);
                var today = DateOnly.FromDateTime(local);
                if (local.Hour >= settings.DocumentScanHourLocal && _lastScan != today)
                {
                    _lastScan = today;
                    await RunLockedAsync("document_expiry_scan", sp => sp.GetRequiredService<DocumentExpiryScanner>().RunOnceAsync(stoppingToken), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification job pass failed");
            }
        }
    }

    private async Task RunLockedAsync(string name, Func<IServiceProvider, Task<int>> run, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return;
        using var scope = scopes.CreateScope();
        await run(scope.ServiceProvider);
    }
}
