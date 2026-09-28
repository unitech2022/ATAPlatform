using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

/// <summary>
/// Anomaly detection on <c>in_trip</c> trips (doc 09 §F12.5) and the "are you OK?" cycle: <c>SafetyMonitorJob</c>, <c>SafetyCheckTimeoutJob</c> and
/// <c>TripShareExpiryJob</c>. Continuity is measured on <c>driver_location_history</c>; a live location older than <c>Matching:LocationMaxAgeSeconds</c>
/// means not enough data (no stop / deviation alert).
/// </summary>
public sealed class SafetyMonitor(
    AtaDbContext db,
    IClock clock,
    SafetyCaseFactory cases,
    TripShareService shares,
    INotificationDispatcher notifications,
    ISafetyNotifier realtime,
    IOptions<SafetyOptions> options,
    IOptions<MatchingOptions> matching,
    ILogger<SafetyMonitor> logger)
{
    private readonly SafetyOptions _options = options.Value;

    public async Task<int> RunMonitorAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var trips = await db.Trips.AsNoTracking().Include(t => t.Stops).Where(t => t.Status == TripStatus.InTrip && t.DriverId != null).ToListAsync(ct);
        var raised = 0;
        foreach (var trip in trips)
        {
            try
            {
                raised += await InspectAsync(trip, now, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Safety monitor failed for trip {TripNumber}", trip.TripNumber);
            }
        }

        return raised;
    }

    private async Task<int> InspectAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        var alerts = await db.SafetyAlerts.AsNoTracking().Where(a => a.TripId == trip.Id).Select(a => new { a.Type, a.Status, a.ClosedAt }).ToListAsync(ct);
        var cooldownAfter = now.AddMinutes(-_options.AlertCooldownMinutes);
        bool Blocked(SafetyAlertType type) => alerts.Any(a => a.Type == type && (a.Status == SafetyAlertStatus.PendingRider || a.ClosedAt > cooldownAfter));

        var location = await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == trip.DriverId, ct);
        var fresh = location is not null && location.UpdatedAt >= now.AddSeconds(-matching.Value.LocationMaxAgeSeconds);
        var raised = 0;

        if (fresh)
        {
            var since = now.AddMinutes(-Math.Max(_options.StopMinutes * 3, 30));
            var points = (await db.DriverLocationHistory.AsNoTracking().Where(h => h.TripId == trip.Id && h.RecordedAt >= since).OrderBy(h => h.RecordedAt)
                    .Select(h => new { h.Lat, h.Lng, h.RecordedAt }).ToListAsync(ct))
                .Select(h => (Lat: (double)h.Lat, Lng: (double)h.Lng, At: h.RecordedAt)).ToList();
            if (points.Count == 0 || points[^1].At < location!.UpdatedAt)
            {
                points.Add(((double)location!.Lat, (double)location.Lng, location.UpdatedAt));
            }

            var current = points[^1];
            if (!Blocked(SafetyAlertType.UnexpectedStop) && (location!.Speed is null || (double)location.Speed < _options.StopSpeedMps))
            {
                var stoppedSince = current.At;
                for (var i = points.Count - 1; i > 0; i--)
                {
                    var seconds = (points[i].At - points[i - 1].At).TotalSeconds;
                    var meters = Geo.HaversineMeters(points[i - 1].Lat, points[i - 1].Lng, points[i].Lat, points[i].Lng);
                    if (seconds <= 0 || meters / seconds >= _options.StopSpeedMps)
                    {
                        break;
                    }

                    stoppedSince = points[i - 1].At;
                }

                var stoppedSeconds = (int)(current.At - stoppedSince).TotalSeconds;
                if (stoppedSeconds > _options.StopMinutes * 60 && FarFromKnownPlaces(trip, current.Lat, current.Lng))
                {
                    await RaiseAsync(trip, SafetyAlertType.UnexpectedStop, current.Lat, current.Lng, new SafetyAlertMetricsDto(StoppedSeconds: stoppedSeconds), now, ct);
                    raised++;
                }
            }

            if (!Blocked(SafetyAlertType.RouteDeviation))
            {
                var route = PlannedRoutes.Of(trip, trip.Stops);
                var threshold = trip.PlannedRouteSource == PlannedRouteSource.Maps ? _options.DeviationMetersMaps : _options.DeviationMeters;
                var currentDistance = PlannedRoutes.DistanceToRouteMeters(current.Lat, current.Lng, route);
                if (currentDistance > threshold)
                {
                    var deviatedSince = current.At;
                    for (var i = points.Count - 2; i >= 0 && PlannedRoutes.DistanceToRouteMeters(points[i].Lat, points[i].Lng, route) > threshold; i--)
                    {
                        deviatedSince = points[i].At;
                    }

                    var deviationSeconds = (int)(current.At - deviatedSince).TotalSeconds;
                    if (deviationSeconds > _options.DeviationSeconds)
                    {
                        await RaiseAsync(trip, SafetyAlertType.RouteDeviation, current.Lat, current.Lng,
                            new SafetyAlertMetricsDto(DeviationMeters: (int)Math.Round(currentDistance), DeviationSeconds: deviationSeconds), now, ct);
                        raised++;
                    }
                }
            }
        }

        if (!Blocked(SafetyAlertType.TripOverrun) && trip.StartedAt is { } startedAt && trip.EstimatedDurationS > 0)
        {
            var elapsed = (now - startedAt).TotalSeconds;
            var estimated = (double)trip.EstimatedDurationS;
            if (elapsed > estimated * _options.OverrunFactor && elapsed - estimated > _options.OverrunMinMinutes * 60)
            {
                await RaiseAsync(trip, SafetyAlertType.TripOverrun, fresh ? (double)location!.Lat : null, fresh ? (double)location!.Lng : null,
                    new SafetyAlertMetricsDto(ElapsedSeconds: (int)elapsed, EstimatedSeconds: trip.EstimatedDurationS), now, ct);
                raised++;
            }
        }

        return raised;
    }

    private bool FarFromKnownPlaces(Trip trip, double lat, double lng)
    {
        var radius = _options.StopIgnoreRadiusMeters;
        if (Geo.HaversineMeters(lat, lng, (double)trip.PickupLat, (double)trip.PickupLng) <= radius) return false;
        if (Geo.HaversineMeters(lat, lng, (double)trip.DropoffLat, (double)trip.DropoffLng) <= radius) return false;
        return trip.Stops.All(s => Geo.HaversineMeters(lat, lng, (double)s.Lat, (double)s.Lng) > radius);
    }

    /// <summary>Creates the <c>pending_rider</c> alert, asks the passenger (push <c>safety.check</c> + <c>SafetyCheck</c>) and tells admins (<c>SafetyAlertRaised</c>).</summary>
    private async Task RaiseAsync(Trip trip, SafetyAlertType type, double? lat, double? lng, SafetyAlertMetricsDto metrics, DateTime now, CancellationToken ct)
    {
        var alert = new SafetyAlert
        {
            TripId = trip.Id,
            Type = type,
            DetectedAt = now,
            Lat = lat is { } la ? Math.Round((decimal)la, 7) : null,
            Lng = lng is { } ln ? Math.Round((decimal)ln, 7) : null,
            Metrics = JsonSerializer.Serialize(metrics, JsonDefaults.Options),
            PromptedAt = now,
            RespondBy = now.AddSeconds(_options.CheckResponseSeconds),
        };
        db.SafetyAlerts.Add(alert);
        var passengerUserId = await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);
        var (ar, en) = SafetyLabels.AlertType(type);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyCheck, passengerUserId,
            NotificationPlaceholders.Of().Localized("alertType", ar, en), "alert", alert.Id,
            new Dictionary<string, object?> { ["alertId"] = alert.Id, ["alertType"] = SafetyLabels.Snake(type), ["tripId"] = trip.Id, ["respondBy"] = alert.RespondBy }), ct);
        await db.SaveChangesAsync(ct);

        await realtime.SafetyCheckAsync(passengerUserId, new SafetyCheckEvent(alert.Id, trip.Id, type, alert.RespondBy), ct);
        await realtime.SafetyAlertRaisedAsync(new AdminSafetyAlertDto(alert.Id, trip.Id, trip.TripNumber, alert.Type, alert.Status, alert.DetectedAt, alert.Lat, alert.Lng, metrics,
            alert.PromptedAt, alert.RespondBy, null, null, null, null, null, alert.CreatedAt), ct);
    }

    /// <summary><c>SafetyCheckTimeoutJob</c>: <c>pending_rider</c> alerts past <c>respond_by</c> become <c>no_response</c> with a <c>high</c> case for operations.</summary>
    public async Task<int> RunCheckTimeoutsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var overdue = await db.SafetyAlerts.Where(a => a.Status == SafetyAlertStatus.PendingRider && a.RespondBy != null && a.RespondBy <= now).ToListAsync(ct);
        foreach (var alert in overdue)
        {
            alert.Close(SafetyAlertStatus.NoResponse, now);
            var passengerUserId = await (from t in db.Trips.AsNoTracking() join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id where t.Id == alert.TripId select p.UserId).FirstAsync(ct);
            var userName = await db.Users.AsNoTracking().Where(u => u.Id == passengerUserId).Select(u => u.FullName ?? u.PhoneNumber).FirstOrDefaultAsync(ct);
            var safetyCase = await cases.AddAsync(CaseFor(alert, null, SafetyReporterRole.System, alert.Lat, alert.Lng, now),
                $"No answer to the {SafetyLabels.Snake(alert.Type)} check within {_options.CheckResponseSeconds} s", notifyOps: true, userName, ct);
            alert.SafetyCaseId = safetyCase.Id;
            await db.SaveChangesAsync(ct);
        }

        await cases.PublishAsync(ct);
        return overdue.Count;
    }

    /// <summary><c>TripShareExpiryJob</c> (links of ended trips) plus the chat retention purge (<c>Retention:TripMessagesDays</c>).</summary>
    public async Task<int> RunShareExpiryAsync(CancellationToken ct)
    {
        var expired = await shares.ExpireEndedAsync(ct);
        var purgeBefore = clock.UtcNow.AddDays(-Math.Max(1, _options.TripMessagesRetentionDays));
        await db.TripMessages.Where(m => m.CreatedAt < purgeBefore).ExecuteDeleteAsync(ct);
        return expired;
    }

    /// <summary>The <c>high</c> case opened for an escalated / unanswered alert (type = the alert type, <c>source = alert</c>).</summary>
    public static SafetyCase CaseFor(SafetyAlert alert, Guid? reporterUserId, SafetyReporterRole role, decimal? lat, decimal? lng, DateTime now) => new()
    {
        CaseNumber = string.Empty,
        Type = alert.Type switch
        {
            SafetyAlertType.UnexpectedStop => SafetyCaseType.UnexpectedStop,
            SafetyAlertType.RouteDeviation => SafetyCaseType.RouteDeviation,
            _ => SafetyCaseType.TripOverrun,
        },
        Source = SafetyCaseSource.Alert,
        Priority = SafetyPriority.High,
        TripId = alert.TripId,
        ReporterUserId = reporterUserId,
        ReporterRole = role,
        Lat = lat,
        Lng = lng,
        LastLat = lat,
        LastLng = lng,
        LastLocationAt = lat is null ? null : now,
        OpenedAt = now,
    };
}

/// <summary>Runs the F12 jobs (monitor every <c>Safety:MonitorIntervalSeconds</c>, check timeouts every 15 s, share expiry every minute); off with <c>Safety:JobsEnabled=false</c>.</summary>
public sealed class SafetyBackgroundService(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<SafetyOptions> options, ILogger<SafetyBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        (string Name, TimeSpan Every, Func<SafetyMonitor, CancellationToken, Task<int>> Run)[] jobs =
        [
            ("safety_monitor", TimeSpan.FromSeconds(Math.Max(5, options.Value.MonitorIntervalSeconds)), (m, ct) => m.RunMonitorAsync(ct)),
            ("safety_check_timeout", TimeSpan.FromSeconds(15), (m, ct) => m.RunCheckTimeoutsAsync(ct)),
            ("trip_share_expiry", TimeSpan.FromMinutes(1), (m, ct) => m.RunShareExpiryAsync(ct)),
        ];
        var next = jobs.ToDictionary(j => j.Name, _ => DateTime.UtcNow);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var job in jobs.Where(j => next[j.Name] <= DateTime.UtcNow))
            {
                next[job.Name] = DateTime.UtcNow + job.Every;
                try
                {
                    await RunOnceAsync(job.Name, job.Run, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Safety job {Job} failed", job.Name);
                }
            }
        }
    }

    public async Task<int> RunOnceAsync(string name, Func<SafetyMonitor, CancellationToken, Task<int>> run, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await run(scope.ServiceProvider.GetRequiredService<SafetyMonitor>(), ct);
    }
}
