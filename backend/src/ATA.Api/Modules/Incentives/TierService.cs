using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Incentives;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Incentives;

/// <summary>Process-wide cache of <c>driver_tier_rules</c> (matcher weights and commission benefits); invalidated by the admin endpoints.</summary>
public sealed class DriverTierRuleCache
{
    private IReadOnlyList<DriverTierRule>? _rows;

    public IReadOnlyList<DriverTierRule>? Rows
    {
        get => Volatile.Read(ref _rows);
        set => Volatile.Write(ref _rows, value);
    }

    public void Invalidate() => Rows = null;
}

/// <summary>Reads the tier rules (cached) for the matcher (<c>norm_tier</c>) and the completion (commission discount).</summary>
public sealed class TierRuleProvider(AtaDbContext db, DriverTierRuleCache cache)
{
    public async Task<IReadOnlyList<DriverTierRule>> RulesAsync(CancellationToken ct)
    {
        var rows = cache.Rows;
        if (rows is null)
        {
            rows = await db.DriverTierRules.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);
            cache.Rows = rows;
        }

        return rows;
    }

    /// <summary>F9 <c>norm_tier</c> per tier from <c>matching_norm</c> (the fixed .25/.5/.75/1 map when a row is missing).</summary>
    public async Task<IReadOnlyDictionary<DriverTier, decimal>> MatchingNormsAsync(CancellationToken ct)
    {
        var rules = await RulesAsync(ct);
        return Enum.GetValues<DriverTier>().ToDictionary(t => t, t => rules.FirstOrDefault(r => r.Tier == t)?.MatchingNorm ?? TierMath.DefaultNorm(t));
    }

    public async Task<decimal> CommissionDiscountPercentAsync(DriverTier tier, CancellationToken ct) =>
        (await RulesAsync(ct)).FirstOrDefault(r => r.Tier == tier)?.CommissionDiscountPercent ?? 0m;
}

/// <summary>
/// Driver tiers (doc 10 §F15.7): weekly recalculation over <c>Tiers:PeriodDays</c> (completed trips, <c>drivers.rating_avg</c>, reliability-profile rates) —
/// the highest tier whose conditions all hold, else bronze — with history and <c>driver.tier_changed</c>; manual overrides; the rules console.
/// </summary>
public sealed class TierService(
    AtaDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    INotificationDispatcher notifications,
    AuditService audit,
    TierRuleProvider provider,
    DriverTierRuleCache cache,
    IOptions<TiersOptions> options)
{
    private readonly TiersOptions _options = options.Value;

    public static (string Ar, string En) TierName(DriverTier tier) => tier switch
    {
        DriverTier.Silver => ("فضي", "Silver"),
        DriverTier.Gold => ("ذهبي", "Gold"),
        DriverTier.Platinum => ("بلاتيني", "Platinum"),
        _ => ("برونزي", "Bronze"),
    };

    /// <summary>The highest tier (by <c>sort_order</c>) whose conditions are all met; bronze otherwise.</summary>
    public static DriverTier Evaluate(IReadOnlyList<DriverTierRule> rules, TierMetrics metrics) =>
        rules.Where(r => r.IsMetBy(metrics)).OrderByDescending(r => r.SortOrder).ThenByDescending(r => r.Tier).Select(r => (DriverTier?)r.Tier).FirstOrDefault() ?? DriverTier.Bronze;

    public async Task<TierMetrics> MetricsAsync(DriverProfile driver, CancellationToken ct)
    {
        var since = clock.UtcNow.AddDays(-_options.PeriodDays);
        var completed = await db.Trips.AsNoTracking().CountAsync(t => t.DriverId == driver.Id && t.Status == TripStatus.Completed && t.CompletedAt >= since, ct);
        var profile = await db.ReliabilityProfiles.AsNoTracking().Where(p => p.UserId == driver.UserId && p.Role == Role.Driver)
            .Select(p => new { p.AcceptanceRate, p.CancellationRate }).FirstOrDefaultAsync(ct);
        return new TierMetrics(completed, driver.RatingAvg, profile?.AcceptanceRate ?? 1m, profile?.CancellationRate ?? 0m);
    }

    /// <summary><c>DriverTierRecalcJob</c> (also <c>POST /admin/driver-tiers/recalculate</c>).</summary>
    public async Task<TierRecalculationDto> RecalculateAllAsync(CancellationToken ct)
    {
        var rules = await provider.RulesAsync(ct);
        var ids = await db.Drivers.AsNoTracking().Where(d => d.ApplicationStatus == ApplicationStatus.Approved || d.ApplicationStatus == ApplicationStatus.Suspended)
            .Select(d => d.Id).ToListAsync(ct);
        var changed = 0;
        foreach (var batch in ids.Chunk(200))
        {
            var drivers = await db.Drivers.Where(d => batch.Contains(d.Id)).ToListAsync(ct);
            foreach (var driver in drivers)
            {
                var metrics = await MetricsAsync(driver, ct);
                var tier = Evaluate(rules, metrics);
                if (tier != driver.Tier)
                {
                    await ChangeAsync(driver, tier, TierChangeReason.WeeklyRecalc, metrics, null, null, ct);
                    changed++;
                }
            }

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }

        return new TierRecalculationDto(ids.Count, changed);
    }

    private async Task ChangeAsync(DriverProfile driver, DriverTier tier, TierChangeReason reason, TierMetrics? metrics, string? note, Guid? changedBy, CancellationToken ct)
    {
        var from = driver.Tier;
        driver.Tier = tier;
        db.DriverTierHistory.Add(new DriverTierHistory
        {
            DriverId = driver.Id, FromTier = from, ToTier = tier, Reason = reason, Note = note, ChangedBy = changedBy, ComputedAt = clock.UtcNow,
            Metrics = metrics is null ? null : JsonSerializer.Serialize(TierMetricsDto.From(metrics), JsonDefaults.Options),
        });
        var (ar, en) = TierName(tier);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.DriverTierChanged, driver.UserId,
            NotificationPlaceholders.Of().Localized("tierName", ar, en), "driver", driver.Id,
            new Dictionary<string, object?> { ["tier"] = JsonNamingPolicy.SnakeCaseLower.ConvertName(tier.ToString()), ["fromTier"] = JsonNamingPolicy.SnakeCaseLower.ConvertName(from.ToString()) }), ct);
    }

    /// <summary><c>GET /driver/tier</c>.</summary>
    public async Task<DriverTierDto> ForDriverAsync(Language lang, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var rules = await provider.RulesAsync(ct);
        var metrics = await MetricsAsync(driver, ct);
        var current = rules.FirstOrDefault(r => r.Tier == driver.Tier);
        var next = rules.Where(r => current is null ? r.Tier > driver.Tier : r.SortOrder > current.SortOrder).OrderBy(r => r.SortOrder).FirstOrDefault();
        return new DriverTierDto(
            driver.Tier, next?.Tier, _options.PeriodDays, TierMetricsDto.From(metrics),
            next is null ? null : new TierRequirementsDto(next.MinCompletedTrips, next.MinRatingAvg, next.MinAcceptanceRate, next.MaxCancellationRate),
            new TierBenefitsDto(current?.CommissionDiscountPercent ?? 0m, lang.PickOptional(current?.BenefitsAr, current?.BenefitsEn)),
            NextRecalculation(clock.UtcNow));
    }

    /// <summary>Next <c>RecalcDayOfWeek</c> at <c>RecalcHourLocal</c> Riyadh time, in UTC.</summary>
    public DateTime NextRecalculation(DateTime utc)
    {
        var local = Formats.ToRiyadh(utc);
        var days = ((_options.RecalcDayOfWeek - (int)local.DayOfWeek) % 7 + 7) % 7;
        var candidate = local.Date.AddDays(days).AddHours(_options.RecalcHourLocal);
        if (candidate <= local)
        {
            candidate = candidate.AddDays(7);
        }

        return DateTime.SpecifyKind(candidate.AddMinutes(-Formats.RiyadhOffsetMinutes), DateTimeKind.Utc);
    }

    // ----- admin -----

    public async Task<IReadOnlyList<TierRuleDto>> RulesAsync(CancellationToken ct)
    {
        var rules = await db.DriverTierRules.AsNoTracking().OrderBy(r => r.SortOrder).ToListAsync(ct);
        var counts = await db.Drivers.AsNoTracking().Where(d => d.ApplicationStatus == ApplicationStatus.Approved)
            .GroupBy(d => d.Tier).Select(g => new { Tier = g.Key, Count = g.Count() }).ToListAsync(ct);
        return rules.Select(r => ToDto(r, counts.FirstOrDefault(c => c.Tier == r.Tier)?.Count ?? 0)).ToList();
    }

    public async Task<TierRuleDto> UpdateRuleAsync(Guid id, TierRuleUpdateRequest r, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(r.MinCompletedTrips), r.MinCompletedTrips)
            .Require(nameof(r.MinRatingAvg), r.MinRatingAvg)
            .Require(nameof(r.MinAcceptanceRate), r.MinAcceptanceRate)
            .Require(nameof(r.MaxCancellationRate), r.MaxCancellationRate)
            .Require(nameof(r.CommissionDiscountPercent), r.CommissionDiscountPercent)
            .Require(nameof(r.MatchingNorm), r.MatchingNorm)
            .Rule(nameof(r.MinCompletedTrips), r.MinCompletedTrips is null or >= 0, "must be positive")
            .Rule(nameof(r.MinRatingAvg), r.MinRatingAvg is null or (>= 0 and <= 5), "must be between 0 and 5")
            .Rule(nameof(r.MinAcceptanceRate), r.MinAcceptanceRate is null or (>= 0 and <= 1), "must be between 0 and 1")
            .Rule(nameof(r.MaxCancellationRate), r.MaxCancellationRate is null or (>= 0 and <= 1), "must be between 0 and 1")
            .Rule(nameof(r.CommissionDiscountPercent), r.CommissionDiscountPercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.MatchingNorm), r.MatchingNorm is null or (>= 0 and <= 1), "must be between 0 and 1")
            .Rule(nameof(r.BenefitsAr), r.BenefitsAr is null || r.BenefitsAr.Length <= 500, "max_length:500")
            .Rule(nameof(r.BenefitsEn), r.BenefitsEn is null || r.BenefitsEn.Length <= 500, "max_length:500")
            .ThrowIfInvalid();
        var rule = Guard.NotFound(await db.DriverTierRules.FirstOrDefaultAsync(x => x.Id == id, ct));
        var before = ToDto(rule, 0);
        rule.MinCompletedTrips = r.MinCompletedTrips!.Value;
        rule.MinRatingAvg = r.MinRatingAvg!.Value;
        rule.MinAcceptanceRate = r.MinAcceptanceRate!.Value;
        rule.MaxCancellationRate = r.MaxCancellationRate!.Value;
        rule.CommissionDiscountPercent = r.CommissionDiscountPercent!.Value;
        rule.MatchingNorm = r.MatchingNorm!.Value;
        rule.BenefitsAr = string.IsNullOrWhiteSpace(r.BenefitsAr) ? null : r.BenefitsAr.Trim();
        rule.BenefitsEn = string.IsNullOrWhiteSpace(r.BenefitsEn) ? null : r.BenefitsEn.Trim();
        rule.SortOrder = r.SortOrder ?? rule.SortOrder;
        audit.Log("driver_tier_rule.update", "driver_tier_rule", rule.Id, before, ToDto(rule, 0));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        var count = await db.Drivers.AsNoTracking().CountAsync(d => d.ApplicationStatus == ApplicationStatus.Approved && d.Tier == rule.Tier, ct);
        return ToDto(rule, count);
    }

    /// <summary>Manual tier until the next recalculation (audited <c>driver.tier_set</c>).</summary>
    public async Task<TierHistoryDto> SetTierAsync(Guid driverId, SetTierRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Tier), request.Tier).Require(nameof(request.Reason), request.Reason, 500).ThrowIfInvalid();
        var driver = Guard.NotFound(await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct));
        var from = driver.Tier;
        var reason = request.Reason!.Trim();
        if (from != request.Tier)
        {
            await ChangeAsync(driver, request.Tier!.Value, TierChangeReason.Admin, null, reason, currentUser.UserId, ct);
        }
        else
        {
            db.DriverTierHistory.Add(new DriverTierHistory
            {
                DriverId = driver.Id, FromTier = from, ToTier = from, Reason = TierChangeReason.Admin, Note = reason, ChangedBy = currentUser.UserId, ComputedAt = clock.UtcNow,
            });
        }

        audit.Log("driver.tier_set", "driver", driver.Id, new { tier = from }, new { tier = driver.Tier, reason });
        await db.SaveChangesAsync(ct);
        return (await HistoryAsync(driverId, ct))[0];
    }

    public async Task<IReadOnlyList<TierHistoryDto>> HistoryAsync(Guid driverId, CancellationToken ct)
    {
        Guard.NotFound(await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct));
        var rows = await db.DriverTierHistory.AsNoTracking().Where(h => h.DriverId == driverId).OrderByDescending(h => h.ComputedAt).ThenByDescending(h => h.Id).ToListAsync(ct);
        var actorIds = rows.Where(h => h.ChangedBy != null).Select(h => h.ChangedBy!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        return rows.Select(h => new TierHistoryDto(h.Id, h.FromTier, h.ToTier,
            h.Metrics is null ? null : JsonSerializer.Deserialize<TierMetricsDto>(h.Metrics, JsonDefaults.Options), h.Reason, h.ComputedAt, h.Note,
            h.ChangedBy is { } by ? names.GetValueOrDefault(by) : null)).ToList();
    }

    private static TierRuleDto ToDto(DriverTierRule r, int count) => new(r.Id, r.Tier, r.MinCompletedTrips, r.MinRatingAvg, r.MinAcceptanceRate, r.MaxCancellationRate,
        r.CommissionDiscountPercent, r.MatchingNorm, r.BenefitsAr, r.BenefitsEn, r.SortOrder, r.UpdatedAt, count);
}
