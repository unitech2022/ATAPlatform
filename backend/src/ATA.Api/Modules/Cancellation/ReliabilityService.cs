using System.Globalization;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Cancellation;

/// <summary>What the other features need to know about a user's reliability (doc 09 §F14.3 "نقاط الربط").</summary>
public sealed record ReliabilitySnapshot(Guid UserId, Role Role, RestrictionLevel Level, DateTime? RestrictedUntil, decimal CancellationRate, int PenaltyPoints,
    decimal? DeprioritizeFactor, decimal? IncentiveReductionPercent, bool Restricted)
{
    public static ReliabilitySnapshot None(Guid userId, Role role) => new(userId, role, RestrictionLevel.None, null, 0m, 0, null, null, false);
}

public interface IReliabilityService
{
    Task<ReliabilitySnapshot> GetAsync(Guid userId, Role role, CancellationToken ct);

    /// <summary>1.0, or the level's <c>deprioritize_factor</c> — multiplies the F9 matching score.</summary>
    decimal MatchingFactor(ReliabilitySnapshot s);

    /// <summary>Temporarily restricted (until <c>restricted_until</c>) or suspended: excluded from matching, cannot request / go online.</summary>
    bool IsRestricted(ReliabilitySnapshot s);

    /// <summary><c>1 − incentive_reduction_percent/100</c> from <c>incentives_reduced</c> upwards — used by F15 incentive payouts.</summary>
    decimal IncentiveMultiplier(ReliabilitySnapshot s);
}

/// <summary>
/// Rolling reliability profiles (doc 09 §F14.3 "ملفات الموثوقية"): counts over <c>Reliability:WindowDays</c>, penalty points over
/// <c>Reliability:PointsExpiryDays</c> (events not waived + manual adjustments, never below 0, reset by <c>clear_restriction</c>), and the
/// restriction ladder of <c>reliability_thresholds</c>.
/// </summary>
public sealed class ReliabilityService(
    AtaDbContext db,
    IClock clock,
    INotificationDispatcher notifications,
    AuditService audit,
    IOptions<ReliabilityOptions> options) : IReliabilityService
{
    private readonly ReliabilityOptions _options = options.Value;

    public async Task<ReliabilitySnapshot> GetAsync(Guid userId, Role role, CancellationToken ct)
    {
        var profile = await db.ReliabilityProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId && p.Role == role, ct);
        if (profile is null)
        {
            return ReliabilitySnapshot.None(userId, role);
        }

        var thresholds = await ThresholdsAsync(role, ct);
        return Snapshot(profile, thresholds, clock.UtcNow);
    }

    public decimal MatchingFactor(ReliabilitySnapshot s) => s.DeprioritizeFactor is { } f && s.Level is RestrictionLevel.MatchingDeprioritized or RestrictionLevel.IncentivesReduced ? f : 1m;

    public bool IsRestricted(ReliabilitySnapshot s) => s.Restricted;

    public decimal IncentiveMultiplier(ReliabilitySnapshot s) =>
        RestrictionLevels.Severity(s.Level) >= RestrictionLevels.Severity(RestrictionLevel.IncentivesReduced) && s.IncentiveReductionPercent is { } pct
            ? Math.Clamp(1m - pct / 100m, 0m, 1m)
            : 1m;

    /// <summary>Throws <c>403 account_restricted { level, restrictedUntil }</c> when the user may not request trips / go online.</summary>
    public async Task EnsureNotRestrictedAsync(Guid userId, Role role, CancellationToken ct)
    {
        var snapshot = await GetAsync(userId, role, ct);
        if (IsRestricted(snapshot))
        {
            throw new DomainException(ErrorCodes.AccountRestricted, new { level = snapshot.Level, restrictedUntil = snapshot.RestrictedUntil });
        }
    }

    public ReliabilitySnapshot Snapshot(ReliabilityProfile profile, IReadOnlyList<ReliabilityThreshold> thresholds, DateTime now)
    {
        var restricted = profile.RestrictionLevel == RestrictionLevel.Suspended
                         || (profile.RestrictionLevel == RestrictionLevel.TemporarilyRestricted && (profile.RestrictedUntil is null || profile.RestrictedUntil > now));
        var own = thresholds.FirstOrDefault(t => t.Level == profile.RestrictionLevel);
        var factor = own?.DeprioritizeFactor ?? thresholds.FirstOrDefault(t => t.Level == RestrictionLevel.MatchingDeprioritized)?.DeprioritizeFactor;
        var reduction = own?.IncentiveReductionPercent ?? thresholds.FirstOrDefault(t => t.Level == RestrictionLevel.IncentivesReduced)?.IncentiveReductionPercent;
        return new ReliabilitySnapshot(profile.UserId, profile.Role, profile.RestrictionLevel, restricted ? profile.RestrictedUntil : null, profile.CancellationRate,
            profile.PenaltyPoints, factor, reduction, restricted);
    }

    public async Task<IReadOnlyList<ReliabilityThreshold>> ThresholdsAsync(Role role, CancellationToken ct) =>
        await db.ReliabilityThresholds.AsNoTracking().Where(t => t.Role == role && t.IsActive).OrderByDescending(t => t.SortOrder).ToListAsync(ct);

    /// <summary>Recomputes and saves the profile (called after the caller's own unit of work was committed).</summary>
    public async Task<ReliabilityProfile?> RefreshAsync(Guid userId, Role role, CancellationToken ct)
    {
        var profile = await RecomputeAsync(userId, role, ct);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    /// <summary>
    /// Recomputes the profile in the current unit of work (the caller saves): counts and rates in the window, points, then the level. A
    /// <c>set_level</c> adjustment still in force wins; <c>temporarily_restricted</c> keeps until <c>restricted_until</c> and is re-entered only after a new
    /// at-fault cancellation; automatic <c>suspended</c> stays until operations intervene.
    /// </summary>
    public async Task<ReliabilityProfile?> RecomputeAsync(Guid userId, Role role, CancellationToken ct)
    {
        if (role is not (Role.Passenger or Role.Driver))
        {
            return null;
        }

        var now = clock.UtcNow;
        var windowStart = now.AddDays(-_options.WindowDays);
        var pointsStart = now.AddDays(-_options.PointsExpiryDays);
        var profile = db.ReliabilityProfiles.Local.FirstOrDefault(p => p.UserId == userId && p.Role == role)
                      ?? await db.ReliabilityProfiles.FirstOrDefaultAsync(p => p.UserId == userId && p.Role == role, ct);
        if (profile is null)
        {
            profile = new ReliabilityProfile { UserId = userId, Role = role };
            db.ReliabilityProfiles.Add(profile);
        }

        var adjustments = await db.ReliabilityAdjustments.AsNoTracking().Where(a => a.UserId == userId && a.Role == role).OrderBy(a => a.CreatedAt).ToListAsync(ct);
        adjustments.AddRange(db.ReliabilityAdjustments.Local.Where(a => a.UserId == userId && a.Role == role && adjustments.All(x => x.Id != a.Id)));
        var lastClear = adjustments.Where(a => a.Action == ReliabilityAction.ClearRestriction).Select(a => (DateTime?)a.CreatedAt).Max();

        List<(DateTime CreatedAt, bool Counts, CancellationStage Stage, ExcuseStatus Excuse, int Points)> events;
        if (role == Role.Driver)
        {
            var driverId = await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct);
            if (driverId is null)
            {
                return null;
            }

            var trips = await db.Trips.AsNoTracking().Where(t => t.DriverId == driverId && t.AssignedAt >= windowStart).Select(t => t.Status).ToListAsync(ct);
            var offers = await db.TripOffers.AsNoTracking().Where(o => o.DriverId == driverId && o.SentAt >= windowStart).Select(o => o.Status).ToListAsync(ct);
            // F17: reservations the driver failed (late release, missed confirmation, no-show) count like at-fault cancellations and as accepted trips (doc 11 §F17.3.10).
            var reservationFaults = await db.ScheduledRideReservations.AsNoTracking()
                .Where(r => r.DriverId == driverId && r.ReleasedAt != null && r.ReleasedAt >= windowStart
                            && (r.Status == ReservationStatus.NoShow
                                || (r.Status == ReservationStatus.Released && (r.IsLateRelease || r.ReleaseReason == ReservationReleaseReason.ConfirmationMissed || r.ReleaseReason == ReservationReleaseReason.FinalConfirmationMissed))))
                .Select(r => new { ReleasedAt = r.ReleasedAt!.Value, r.Status, r.PenaltyPoints }).ToListAsync(ct);
            profile.TripsAccepted = trips.Count + reservationFaults.Count;
            profile.TripsCompleted = trips.Count(s => s == TripStatus.Completed);
            profile.OffersReceived = offers.Count;
            profile.OffersAccepted = offers.Count(s => s == OfferStatus.Accepted);
            profile.TripsRequested = offers.Count;
            profile.AcceptanceRate = offers.Count == 0 ? null : Rate(profile.OffersAccepted, offers.Count);
            events = (await (from e in db.CancellationEvents.AsNoTracking()
                             join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                             where t.DriverId == driverId && e.AtFault == AtFault.Driver && e.CreatedAt >= windowStart
                             select new { e.CreatedAt, e.CountsTowardRate, e.Stage, e.ExcuseStatus, e.PenaltyPoints }).ToListAsync(ct))
                .Select(e => (e.CreatedAt, e.CountsTowardRate, e.Stage, e.ExcuseStatus, e.PenaltyPoints)).ToList();
            events.AddRange(reservationFaults.Select(f => (f.ReleasedAt, true, f.Status == ReservationStatus.NoShow ? CancellationStage.NoShow : CancellationStage.Scheduled, ExcuseStatus.NotApplicable, f.PenaltyPoints)));
        }
        else
        {
            var passengerId = await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
            if (passengerId is null)
            {
                return null;
            }

            var trips = await db.Trips.AsNoTracking().Where(t => t.PassengerId == passengerId && t.RequestedAt >= windowStart)
                .Select(t => new { t.Status, t.AssignedAt }).ToListAsync(ct);
            profile.TripsRequested = trips.Count;
            profile.TripsAccepted = trips.Count(t => t.AssignedAt != null);
            profile.TripsCompleted = trips.Count(t => t.Status == TripStatus.Completed);
            profile.OffersReceived = 0;
            profile.OffersAccepted = 0;
            profile.AcceptanceRate = null;
            events = (await (from e in db.CancellationEvents.AsNoTracking()
                             join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                             where t.PassengerId == passengerId && e.AtFault == AtFault.Passenger && e.CreatedAt >= windowStart
                             select new { e.CreatedAt, e.CountsTowardRate, e.Stage, e.ExcuseStatus, e.PenaltyPoints }).ToListAsync(ct))
                .Select(e => (e.CreatedAt, e.CountsTowardRate, e.Stage, e.ExcuseStatus, e.PenaltyPoints)).ToList();
        }

        profile.WindowDays = _options.WindowDays;
        profile.CancellationsAtFault = events.Count(e => e.Counts);
        profile.NoShowCount = events.Count(e => e.Counts && e.Stage == CancellationStage.NoShow);
        profile.CancellationRate = profile.TripsAccepted == 0 ? 0m : Math.Min(1m, Rate(profile.CancellationsAtFault, profile.TripsAccepted));
        profile.ReliabilityRate = profile.TripsAccepted == 0 ? 1m : Math.Min(1m, Rate(profile.TripsCompleted, profile.TripsAccepted));

        var eventPoints = events.Where(e => e.CreatedAt >= pointsStart && e.Excuse is not (ExcuseStatus.Pending or ExcuseStatus.Approved) && (lastClear is null || e.CreatedAt > lastClear))
            .Sum(e => e.Points);
        var adjustedPoints = adjustments.Where(a => a.Action is ReliabilityAction.AddPoints or ReliabilityAction.RemovePoints && a.CreatedAt >= pointsStart && (lastClear is null || a.CreatedAt > lastClear))
            .Sum(a => a.Points ?? 0);
        profile.PenaltyPoints = Math.Max(0, eventPoints + adjustedPoints);
        profile.LastComputedAt = now;

        var thresholds = await ThresholdsAsync(role, ct);
        var previous = profile.RestrictionLevel;
        var (level, until) = ResolveLevel(profile, thresholds, adjustments, lastClear, events.Where(e => e.Counts).Select(e => e.CreatedAt).ToList(), now);
        profile.RestrictedUntil = until;
        if (level != previous)
        {
            profile.RestrictionLevel = level;
            profile.LevelChangedAt = now;
            await OnLevelChangedAsync(profile, previous, ct);
        }

        return profile;
    }

    private (RestrictionLevel Level, DateTime? Until) ResolveLevel(ReliabilityProfile profile, IReadOnlyList<ReliabilityThreshold> thresholds, IReadOnlyList<ReliabilityAdjustment> adjustments,
        DateTime? lastClear, IReadOnlyList<DateTime> offences, DateTime now)
    {
        var forced = adjustments
            .Where(a => a.Action == ReliabilityAction.SetLevel && a.Level is not null && (lastClear is null || a.CreatedAt > lastClear) && (a.Until is null || a.Until > now))
            .OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        if (forced is not null)
        {
            var forcedLevel = forced.Level!.Value;
            return (forcedLevel, RestrictionLevels.IsRestricting(forcedLevel) ? forced.Until : profile.RestrictedUntil);
        }

        var current = profile.RestrictionLevel;
        var clearedSinceChange = lastClear is { } cleared && (profile.LevelChangedAt is null || cleared >= profile.LevelChangedAt);
        // A served (or cleared) restriction is remembered in restricted_until: re-entering a restricting level needs a new at-fault cancellation.
        var servedUntil = clearedSinceChange ? lastClear : profile.RestrictedUntil;
        var skipRestricting = servedUntil is { } served && served <= now && !offences.Any(o => o > served);
        var computed = thresholds
            .Where(t => !(skipRestricting && RestrictionLevels.IsRestricting(t.Level)))
            .FirstOrDefault(t => t.IsMetBy(profile.PenaltyPoints, profile.TripsAccepted, profile.CancellationRate));
        var level = computed?.Level ?? RestrictionLevel.None;

        var sticky = !clearedSinceChange && RestrictionLevels.IsRestricting(current)
                     && (profile.RestrictedUntil > now || (current == RestrictionLevel.Suspended && profile.RestrictedUntil is null));
        if (sticky && RestrictionLevels.Severity(level) <= RestrictionLevels.Severity(current))
        {
            return (current, profile.RestrictedUntil);
        }

        return level switch
        {
            RestrictionLevel.TemporarilyRestricted when current != RestrictionLevel.TemporarilyRestricted || profile.RestrictedUntil is null || profile.RestrictedUntil <= now =>
                (level, now.AddHours(computed?.RestrictionHours ?? 24)),
            RestrictionLevel.TemporarilyRestricted => (level, profile.RestrictedUntil),
            RestrictionLevel.Suspended => (level, null),
            // Below the restrictions the end of the last restriction is kept as history (clients only see it while restricted).
            _ => (level, servedUntil is { } end && end <= now ? end : profile.RestrictedUntil),
        };
    }

    private async Task OnLevelChangedAsync(ReliabilityProfile profile, RestrictionLevel previous, CancellationToken ct)
    {
        var log = audit.Log("reliability.level_change", "reliability_profile", profile.Id, new { level = previous }, new { level = profile.RestrictionLevel, profile.RestrictedUntil, profile.PenaltyPoints, profile.CancellationRate });
        log.ActorRole ??= "system";
        var increased = RestrictionLevels.Severity(profile.RestrictionLevel) > RestrictionLevels.Severity(previous);
        var (levelAr, levelEn) = LevelLabel(profile.RestrictionLevel);
        if (increased && RestrictionLevels.IsRestricting(profile.RestrictionLevel))
        {
            var untilAr = profile.RestrictedUntil is { } u ? Formats.LocalTime(u) : "إشعار آخر";
            var untilEn = profile.RestrictedUntil is { } u2 ? Formats.LocalTime(u2) : "further notice";
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.ReliabilityRestricted, profile.UserId,
                NotificationPlaceholders.Of().Localized("level", levelAr, levelEn).Localized("restrictedUntil", untilAr, untilEn), "reliability", profile.Id,
                new Dictionary<string, object?> { ["role"] = profile.Role.ToString().ToLowerInvariant(), ["level"] = Snake(profile.RestrictionLevel) }), ct);
            if (profile.Role == Role.Driver)
            {
                await TakeDriverOfflineAsync(profile.UserId, ct);
            }
        }
        else if (increased)
        {
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.ReliabilityWarning, profile.UserId,
                NotificationPlaceholders.Of(("cancellationRate", profile.CancellationRate.ToString("P0", CultureInfo.InvariantCulture))).Localized("level", levelAr, levelEn),
                "reliability", profile.Id, new Dictionary<string, object?> { ["role"] = profile.Role.ToString().ToLowerInvariant(), ["level"] = Snake(profile.RestrictionLevel) }), ct);
        }
    }

    /// <summary>A restricted driver who is online without a trip is taken offline.</summary>
    private async Task TakeDriverOfflineAsync(Guid userId, CancellationToken ct)
    {
        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (driver is not { IsOnline: true, CurrentTripId: null })
        {
            return;
        }

        var now = clock.UtcNow;
        driver.IsOnline = false;
        driver.LastOnlineAt = now;
        db.DriverStatusLogs.Add(new DriverStatusLog { DriverId = driver.Id, IsOnline = false, ChangedAt = now });
        var location = await db.DriverLocations.FirstOrDefaultAsync(l => l.DriverId == driver.Id, ct);
        if (location is not null)
        {
            location.IsOnline = false;
        }
    }

    /// <summary><c>RestrictionExpiryJob</c>: lifts temporary restrictions whose <c>restricted_until</c> passed and set-level overrides that ended.</summary>
    public async Task<int> ExpireRestrictionsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await db.ReliabilityProfiles.AsNoTracking()
            .Where(p => (p.RestrictionLevel == RestrictionLevel.TemporarilyRestricted || p.RestrictionLevel == RestrictionLevel.Suspended) && p.RestrictedUntil != null && p.RestrictedUntil <= now)
            .Select(p => new { p.UserId, p.Role }).ToListAsync(ct);
        var ended = await db.ReliabilityAdjustments.AsNoTracking().Where(a => a.Action == ReliabilityAction.SetLevel && a.Until != null && a.Until <= now && a.Until > now.AddDays(-1))
            .Select(a => new { a.UserId, a.Role }).ToListAsync(ct);
        var targets = due.Concat(ended).Distinct().ToList();
        foreach (var target in targets)
        {
            await RefreshAsync(target.UserId, target.Role, ct);
        }

        return targets.Count;
    }

    /// <summary><c>ReliabilityRecalcJob</c>: every profile with activity in the window or a restriction in force.</summary>
    public async Task<int> RecalculateAllAsync(CancellationToken ct)
    {
        var since = clock.UtcNow.AddDays(-_options.WindowDays);
        var passengerUsers = await (from t in db.Trips.AsNoTracking() join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id where t.RequestedAt >= since select p.UserId).Distinct().ToListAsync(ct);
        var driverUsers = await (from t in db.Trips.AsNoTracking() join d in db.Drivers.AsNoTracking() on t.DriverId equals d.Id where t.AssignedAt >= since select d.UserId).Distinct().ToListAsync(ct);
        var offerUsers = await (from o in db.TripOffers.AsNoTracking() join d in db.Drivers.AsNoTracking() on o.DriverId equals d.Id where o.SentAt >= since select d.UserId).Distinct().ToListAsync(ct);
        var restricted = await db.ReliabilityProfiles.AsNoTracking().Where(p => p.RestrictionLevel != RestrictionLevel.None).Select(p => new { p.UserId, p.Role }).ToListAsync(ct);
        var targets = passengerUsers.Select(u => (u, Role.Passenger))
            .Concat(driverUsers.Concat(offerUsers).Select(u => (u, Role.Driver)))
            .Concat(restricted.Select(r => (r.UserId, r.Role)))
            .Distinct().ToList();
        foreach (var (userId, role) in targets)
        {
            await RefreshAsync(userId, role, ct);
            db.ChangeTracker.Clear();
        }

        return targets.Count;
    }

    public ReliabilityNextLevelDto? NextLevel(ReliabilityProfile profile, IReadOnlyList<ReliabilityThreshold> thresholds) =>
        thresholds.OrderBy(t => t.SortOrder)
            .Where(t => RestrictionLevels.Severity(t.Level) > RestrictionLevels.Severity(profile.RestrictionLevel))
            .Select(t => new ReliabilityNextLevelDto(t.Level, t.MinPenaltyPoints, t.MinCancellationRate))
            .FirstOrDefault();

    public static (string Ar, string En) LevelLabel(RestrictionLevel level) => level switch
    {
        RestrictionLevel.Warning => ("تنبيه", "warning"),
        RestrictionLevel.MatchingDeprioritized => ("أولوية أقل في الطلبات", "lower matching priority"),
        RestrictionLevel.IncentivesReduced => ("حوافز مخفضة", "reduced incentives"),
        RestrictionLevel.TemporarilyRestricted => ("تقييد مؤقت", "temporarily restricted"),
        RestrictionLevel.Suspended => ("موقوف", "suspended"),
        _ => ("طبيعي", "normal"),
    };

    private static decimal Rate(int part, int whole) => whole == 0 ? 0m : decimal.Round((decimal)part / whole, 4, MidpointRounding.AwayFromZero);

    private static string Snake(RestrictionLevel level) => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(level.ToString());
}

/// <summary>
/// The F14 implementation of the F9 hook: <c>norm_cancellation = 1 − reliability_profiles.cancellation_rate</c>, the level's matching factor and
/// the restriction block. Drivers without a profile keep the counter-based figures of <see cref="CounterDriverReliability"/>.
/// </summary>
public sealed class ProfileDriverReliability(AtaDbContext db, ReliabilityService reliability, CounterDriverReliability counters) : IDriverReliabilityProvider
{
    public async Task<IReadOnlyDictionary<Guid, DriverReliability>> GetAsync(IReadOnlyCollection<Guid> driverIds, DateTime now, CancellationToken ct)
    {
        var baseline = await counters.GetAsync(driverIds, now, ct);
        var ids = driverIds.ToList();
        if (ids.Count == 0)
        {
            return baseline;
        }

        var profiles = await (from d in db.Drivers.AsNoTracking()
                              join p in db.ReliabilityProfiles.AsNoTracking() on d.UserId equals p.UserId
                              where ids.Contains(d.Id) && p.Role == Role.Driver
                              select new { DriverId = d.Id, Profile = p }).ToListAsync(ct);
        if (profiles.Count == 0)
        {
            return baseline;
        }

        var thresholds = await reliability.ThresholdsAsync(Role.Driver, ct);
        var result = new Dictionary<Guid, DriverReliability>(baseline);
        foreach (var row in profiles)
        {
            var snapshot = reliability.Snapshot(row.Profile, thresholds, now);
            var acceptance = baseline.TryGetValue(row.DriverId, out var b) ? b.AcceptanceRate : 1m;
            result[row.DriverId] = new DriverReliability(acceptance, row.Profile.CancellationRate, reliability.IsRestricted(snapshot), reliability.MatchingFactor(snapshot));
        }

        return result;
    }
}

/// <summary>Runs <c>ReliabilityRecalcJob</c> (daily at <c>Reliability:RecalcHourLocal</c> Riyadh) and <c>RestrictionExpiryJob</c> (every 5 minutes).</summary>
public sealed class ReliabilityBackgroundService(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<ReliabilityOptions> options, IClock clock, ILogger<ReliabilityBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        DateOnly? lastRecalc = null;
        var nextExpiry = DateTime.UtcNow;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (DateTime.UtcNow >= nextExpiry)
                {
                    nextExpiry = DateTime.UtcNow.AddMinutes(5);
                    await RunOnceAsync("restriction_expiry", (s, ct) => s.ExpireRestrictionsAsync(ct), stoppingToken);
                }

                var local = Formats.ToRiyadh(clock.UtcNow);
                var today = DateOnly.FromDateTime(local);
                if (local.Hour >= options.Value.RecalcHourLocal && lastRecalc != today)
                {
                    lastRecalc = today;
                    await RunOnceAsync("reliability_recalc", (s, ct) => s.RecalculateAllAsync(ct), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reliability job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(string name, Func<ReliabilityService, CancellationToken, Task<int>> run, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(30), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await run(scope.ServiceProvider.GetRequiredService<ReliabilityService>(), ct);
    }
}
