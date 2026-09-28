using System.Collections.Concurrent;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Pricing;

/// <summary>In-memory demand state: levels and rules (invalidated on admin writes) plus the latest reading per zone/category.</summary>
public sealed class DemandCache
{
    public sealed record Entry(string LevelCode, DateTime ComputedAt, decimal Ratio, int RequestsCount, int OnlineDrivers);

    private readonly ConcurrentDictionary<(Guid ZoneId, Guid? CategoryId), Entry> _latest = new();
    private IReadOnlyList<DemandLevel>? _levels;
    private IReadOnlyList<DemandRule>? _rules;

    public IReadOnlyList<DemandLevel>? Levels
    {
        get => Volatile.Read(ref _levels);
        set => Volatile.Write(ref _levels, value);
    }

    public IReadOnlyList<DemandRule>? Rules
    {
        get => Volatile.Read(ref _rules);
        set => Volatile.Write(ref _rules, value);
    }

    public Entry? Latest(Guid zoneId, Guid? categoryId) => _latest.TryGetValue((zoneId, categoryId), out var e) ? e : null;

    public void Store(Guid zoneId, Guid? categoryId, Entry entry) => _latest[(zoneId, categoryId)] = entry;

    /// <summary>Drops cached levels, rules and readings (overrides are always read from the database).</summary>
    public void Invalidate()
    {
        Levels = null;
        Rules = null;
        _latest.Clear();
    }
}

/// <summary>Current demand level: manual override → latest fresh snapshot → <c>normal</c>, capped by the zone's <c>surge_cap</c>.</summary>
public sealed class DemandService(AtaDbContext db, DemandCache cache, IOptions<DemandOptions> options, IClock clock)
{
    private readonly DemandOptions _options = options.Value;

    public async Task<IReadOnlyList<DemandLevel>> LevelsAsync(CancellationToken ct)
    {
        var levels = cache.Levels;
        if (levels is null)
        {
            levels = await db.DemandLevels.AsNoTracking().OrderBy(l => l.SortOrder).ToListAsync(ct);
            cache.Levels = levels;
        }

        return levels;
    }

    public async Task<IReadOnlyList<DemandRule>> RulesAsync(CancellationToken ct)
    {
        var rules = cache.Rules;
        if (rules is null)
        {
            rules = await db.DemandRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
            cache.Rules = rules;
        }

        return rules;
    }

    /// <summary>The rule for a zone/category: zone+category → zone → category → global.</summary>
    public async Task<DemandRule?> RuleForAsync(Guid zoneId, Guid? rideCategoryId, CancellationToken ct)
    {
        var rules = await RulesAsync(ct);
        return rules
            .Where(r => (r.ZoneId == null || r.ZoneId == zoneId) && (r.RideCategoryId == null || r.RideCategoryId == rideCategoryId))
            .OrderByDescending(r => (r.ZoneId != null ? 2 : 0) + (r.RideCategoryId != null ? 1 : 0))
            .ThenByDescending(r => r.UpdatedAt)
            .FirstOrDefault();
    }

    public async Task<DemandReading> ReadAsync(ZoneSnapshot? zone, Guid? rideCategoryId, DateTime atUtc, CancellationToken ct)
    {
        var levels = await LevelsAsync(ct);
        var normal = levels.FirstOrDefault(l => l.Code == DemandLevel.Normal);
        if (zone is null)
        {
            return DemandReading.Neutral(normal);
        }

        var manual = await db.DemandOverrides.AsNoTracking()
            .Where(o => o.ZoneId == zone.Id && o.StartsAt <= atUtc && o.EndsAt > atUtc && (o.RideCategoryId == null || o.RideCategoryId == rideCategoryId))
            .OrderByDescending(o => o.RideCategoryId != null)
            .ThenByDescending(o => o.StartsAt)
            .FirstOrDefaultAsync(ct);
        if (manual is not null)
        {
            var level = levels.FirstOrDefault(l => l.Id == manual.DemandLevelId) ?? normal;
            return Build(level, zone, rideCategoryId, DemandSource.Override, manual.StartsAt, null, normal);
        }

        var snapshot = await LatestSnapshotAsync(zone.Id, rideCategoryId, atUtc, ct);
        if (snapshot is not null)
        {
            var level = levels.FirstOrDefault(l => l.Code == snapshot.LevelCode) ?? normal;
            return Build(level, zone, rideCategoryId, DemandSource.Snapshot, snapshot.ComputedAt, snapshot.Ratio, normal);
        }

        return Build(normal, zone, rideCategoryId, DemandSource.Default, null, null, normal);
    }

    /// <summary>Latest reading for the category, else for the whole zone, that is not older than <c>Demand:SnapshotMaxAgeMinutes</c>.</summary>
    public async Task<DemandCache.Entry?> LatestSnapshotAsync(Guid zoneId, Guid? rideCategoryId, DateTime atUtc, CancellationToken ct)
    {
        var notBefore = atUtc.AddMinutes(-_options.SnapshotMaxAgeMinutes);
        var live = Math.Abs((clock.UtcNow - atUtc).TotalSeconds) < 60;
        Guid?[] keys = rideCategoryId is null ? [null] : [rideCategoryId, null];
        foreach (var category in keys)
        {
            if (live && cache.Latest(zoneId, category) is { } cached && cached.ComputedAt >= notBefore && cached.ComputedAt <= atUtc)
            {
                return cached;
            }

            var row = await db.DemandSnapshots.AsNoTracking()
                .Where(s => s.ZoneId == zoneId && s.RideCategoryId == category && s.ComputedAt <= atUtc && s.ComputedAt >= notBefore)
                .OrderByDescending(s => s.ComputedAt)
                .Select(s => new DemandCache.Entry(s.DemandLevelCode, s.ComputedAt, s.Ratio, s.RequestsCount, s.OnlineDrivers))
                .FirstOrDefaultAsync(ct);
            if (row is not null)
            {
                if (live)
                {
                    cache.Store(zoneId, category, row);
                }

                return row;
            }
        }

        return null;
    }

    private static DemandReading Build(DemandLevel? level, ZoneSnapshot zone, Guid? rideCategoryId, DemandSource source, DateTime? computedAt, decimal? ratio, DemandLevel? normal)
    {
        if (level is null)
        {
            return DemandReading.Neutral(normal) with { Source = source, ComputedAt = computedAt, Ratio = ratio };
        }

        var capped = Math.Min(level.Multiplier, zone.SurgeCapFor(rideCategoryId));
        return new DemandReading(level.Code, level.NameAr, level.NameEn, capped, level.Multiplier, level.Color, source, computedAt, ratio);
    }
}

public sealed record DemandChangedEvent(Guid ZoneId, string ZoneCode, Guid? RideCategoryId, string PreviousCode, string Code, decimal Multiplier, string Color, decimal Ratio, int RequestsCount, int OnlineDrivers, DateTime ComputedAt);

/// <summary>
/// One demand pass: for every active zone (and category when a rule targets one) counts <c>requested/searching</c> trips in the rule's
/// window against available online drivers, maps the ratio to a level, stores a <c>demand_snapshots</c> row and pushes
/// <c>DemandChanged</c> to admins when the level moved.
/// </summary>
public sealed class DemandComputer(
    AtaDbContext db,
    ZoneResolver zones,
    DemandService demand,
    DemandCache cache,
    ITripNotifier notifier,
    IOptions<DemandOptions> demandOptions,
    IOptions<MatchingOptions> matchingOptions,
    IClock clock)
{
    private readonly DemandOptions _options = demandOptions.Value;
    private readonly MatchingOptions _matching = matchingOptions.Value;

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var zoneList = await zones.AllAsync(ct);
        var rules = await demand.RulesAsync(ct);
        var levels = await demand.LevelsAsync(ct);
        if (zoneList.Count == 0 || rules.Count == 0)
        {
            return 0;
        }

        var maxWindow = rules.Max(r => r.WindowMinutes);
        var since = now.AddMinutes(-maxWindow);
        var requests = await db.Trips.AsNoTracking()
            .Where(t => (t.Status == TripStatus.Requested || t.Status == TripStatus.Searching) && t.RequestedAt >= since)
            .Select(t => new { t.PickupLat, t.PickupLng, t.RideCategoryId, t.RequestedAt })
            .ToListAsync(ct);
        var freshAfter = now.AddSeconds(-_matching.LocationMaxAgeSeconds);
        var drivers = await (from loc in db.DriverLocations.AsNoTracking()
                             join d in db.Drivers.AsNoTracking() on loc.DriverId equals d.Id
                             join v in db.Vehicles.AsNoTracking() on d.Id equals v.DriverId
                             where loc.IsOnline && d.IsOnline && d.ApplicationStatus == ApplicationStatus.Approved && d.CurrentTripId == null
                                   && v.IsActive && loc.UpdatedAt >= freshAfter
                             select new { loc.Lat, loc.Lng, v.RideCategoryId }).ToListAsync(ct);

        var requestZones = requests.Select(r => (Zone: zones.Resolve(zoneList, r.PickupLat, r.PickupLng, now)?.Id, r.RideCategoryId, r.RequestedAt)).ToList();
        var driverZones = drivers.Select(d => (Zone: zones.Resolve(zoneList, d.Lat, d.Lng, now)?.Id, d.RideCategoryId)).ToList();

        var changes = new List<DemandChangedEvent>();
        var written = 0;
        foreach (var zone in zoneList)
        {
            var applicable = rules
                .Where(r => r.ZoneId == null || r.ZoneId == zone.Id)
                .GroupBy(r => r.RideCategoryId)
                .Select(g => g.OrderByDescending(r => r.ZoneId != null).ThenByDescending(r => r.UpdatedAt).First());
            foreach (var rule in applicable)
            {
                var windowStart = now.AddMinutes(-rule.WindowMinutes);
                var requestsCount = requestZones.Count(r => r.Zone == zone.Id && r.RequestedAt >= windowStart && (rule.RideCategoryId == null || r.RideCategoryId == rule.RideCategoryId));
                var onlineDrivers = driverZones.Count(d => d.Zone == zone.Id && (rule.RideCategoryId == null || d.RideCategoryId == rule.RideCategoryId));
                var ratio = onlineDrivers == 0 ? requestsCount : decimal.Round((decimal)requestsCount / onlineDrivers, 3, MidpointRounding.AwayFromZero);
                var code = rule.LevelFor(ratio);

                var previous = (await demand.LatestSnapshotAsync(zone.Id, rule.RideCategoryId, now, ct))?.LevelCode ?? DemandLevel.Normal;
                db.DemandSnapshots.Add(new DemandSnapshot
                {
                    ZoneId = zone.Id, RideCategoryId = rule.RideCategoryId, ComputedAt = now, RequestsCount = requestsCount, OnlineDrivers = onlineDrivers, Ratio = ratio, DemandLevelCode = code,
                });
                cache.Store(zone.Id, rule.RideCategoryId, new DemandCache.Entry(code, now, ratio, requestsCount, onlineDrivers));
                written++;
                if (code != previous)
                {
                    var level = levels.FirstOrDefault(l => l.Code == code);
                    changes.Add(new DemandChangedEvent(zone.Id, zone.Code, rule.RideCategoryId, previous, code,
                        Math.Min(level?.Multiplier ?? 1m, zone.SurgeCapFor(rule.RideCategoryId)), level?.Color ?? "#19B7A5", ratio, requestsCount, onlineDrivers, now));
                }
            }
        }

        var retention = now.AddHours(-_options.SnapshotRetentionHours);
        await db.DemandSnapshots.Where(s => s.ComputedAt < retention).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        foreach (var change in changes)
        {
            await notifier.DemandChangedAsync(change, ct);
        }

        return written;
    }
}

/// <summary>Computes demand snapshots every <c>Demand:IntervalSeconds</c>; disabled with <c>Demand:Enabled=false</c> (tests call <see cref="RunOnceAsync"/>).</summary>
public sealed class DemandBackgroundService(IServiceScopeFactory scopes, IOptions<DemandOptions> options, ILogger<DemandBackgroundService> logger) : BackgroundService
{
    private readonly DemandOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demand pass failed");
            }
        }
    }

    internal async Task<int> RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DemandComputer>().RunOnceAsync(ct);
    }
}
