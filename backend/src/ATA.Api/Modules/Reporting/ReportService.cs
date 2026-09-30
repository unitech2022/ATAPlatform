using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Reporting;
using ATA.Domain.Safety;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Reporting;

/// <summary>
/// <c>/admin/reports</c> (doc 12 §F20.7). <c>sum</c>/<c>ratio</c>/<c>avg</c> metrics aggregate the daily <c>report_snapshots</c> (Σ values, Σ numerator ÷ Σ
/// denominator); days without a snapshot (today, or days the job has not covered yet) are computed live the same way. <c>distinct</c> metrics are always
/// computed directly from the tables for the requested range (the daily snapshot is only used for the daily series). Metrics without a place / category
/// dimension return <c>null</c> when a filter is set.
/// </summary>
public sealed class ReportService(AtaDbContext db, KpiCalculator calculator, IClock clock, IOptions<ReportsOptions> options)
{
    public const string ComparePreviousPeriod = "previous_period";

    private sealed record DailyCell(DateOnly Date, string Scope, string Metric, decimal Value, decimal? Numerator, decimal? Denominator);

    public IReadOnlyList<KpiDefinitionDto> Definitions(Language lang) =>
        KpiCodes.All.Select(d => new KpiDefinitionDto(d.Code, lang.Pick(d.NameAr, d.NameEn), d.NameAr, d.NameEn, d.Unit, d.Aggregation, d.Group, d.Dimensional)).ToList();

    public async Task<KpisResponse> KpisAsync(DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, string? compare, string? metrics, Language lang, CancellationToken ct)
    {
        var (start, end) = ValidateRange(from, to);
        var scope = await ValidateScopeAsync(cityId, zoneId, rideCategoryId, ct);
        new Validator().Rule(nameof(compare), string.IsNullOrWhiteSpace(compare) || compare == ComparePreviousPeriod, $"must be {ComparePreviousPeriod}").ThrowIfInvalid();
        var definitions = ParseMetrics(metrics);
        var current = await AggregateRangeAsync(start, end, scope, definitions, ct);
        IReadOnlyDictionary<string, KpiCell>? previous = null;
        DateOnly? previousFrom = null, previousTo = null;
        if (compare == ComparePreviousPeriod)
        {
            var days = end.DayNumber - start.DayNumber + 1;
            previousTo = start.AddDays(-1);
            previousFrom = previousTo.Value.AddDays(-(days - 1));
            previous = await AggregateRangeAsync(previousFrom.Value, previousTo.Value, scope, definitions, ct);
        }

        var items = definitions.Select(d =>
        {
            var cell = current[d.Code];
            var value = KpiMath.Display(d, cell.Value);
            var previousValue = previous is null ? null : KpiMath.Display(d, previous[d.Code].Value);
            return new KpiMetricDto(d.Code, lang.Pick(d.NameAr, d.NameEn), d.Unit, d.Aggregation, d.Group, value, previousValue, KpiMath.ChangePercent(value, previousValue),
                cell.Numerator, cell.Denominator);
        }).ToList();
        return new KpisResponse(start, end, new KpiFiltersDto(cityId, zoneId, rideCategoryId), string.IsNullOrWhiteSpace(compare) ? null : compare, previousFrom, previousTo, items);
    }

    public async Task<KpiSeriesResponse> SeriesAsync(string code, DateOnly? from, DateOnly? to, string? granularity, Guid? cityId, Guid? zoneId, Guid? rideCategoryId,
        Language lang, CancellationToken ct)
    {
        var definition = KpiCodes.ByCode.GetValueOrDefault(code) ?? throw new DomainException(ErrorCodes.NotFound, new { code });
        var (start, end) = ValidateRange(from, to);
        var scope = await ValidateScopeAsync(cityId, zoneId, rideCategoryId, ct);
        var grain = QueryEnum.Parse<KpiGranularity>(granularity, nameof(granularity)) ?? KpiGranularity.Day;
        Func<DateOnly, DateOnly> bucketOf = grain switch
        {
            KpiGranularity.Week => d => d.AddDays(-(int)d.DayOfWeek),
            KpiGranularity.Month => d => new DateOnly(d.Year, d.Month, 1),
            _ => d => d,
        };
        var buckets = new List<DateOnly>();
        for (var b = bucketOf(start); b <= end; b = grain switch { KpiGranularity.Week => b.AddDays(7), KpiGranularity.Month => b.AddMonths(1), _ => b.AddDays(1) })
        {
            buckets.Add(b);
        }

        Dictionary<DateOnly, KpiCell> values;
        if (!ApplicableTo(definition, scope))
        {
            values = [];
        }
        else if (definition.Aggregation == KpiAggregation.Distinct && grain != KpiGranularity.Day)
        {
            var computation = await calculator.ComputeAsync(start, end, bucketOf, new HashSet<string> { definition.Code }, ct);
            values = buckets.ToDictionary(b => b, b => KpiCell.From(definition, computation.Get(b, scope.Key, definition.Code)));
        }
        else
        {
            var cells = await DailyCellsAsync(start, end, [scope.Key], [definition], ct);
            values = cells.GroupBy(c => bucketOf(c.Date)).ToDictionary(g => g.Key, g => Aggregate(definition, g.ToList()));
        }

        var points = buckets.Select(b =>
        {
            var cell = values.GetValueOrDefault(b) ?? (ApplicableTo(definition, scope) ? KpiCell.From(definition, null) : new KpiCell(null, null, null));
            return new KpiPointDto(b, KpiMath.Display(definition, cell.Value), cell.Numerator, cell.Denominator);
        }).ToList();
        return new KpiSeriesResponse(definition.Code, lang.Pick(definition.NameAr, definition.NameEn), definition.Unit, grain, start, end, points);
    }

    /// <summary>
    /// <c>GET /admin/reports/breakdown</c>: one row per city / zone / category. The KPI filters narrow it (dashboard addition): a category filter uses the
    /// <c>city|cat</c> / <c>zone|cat</c> scopes, a city filter keeps its zones (or, grouped by category, uses <c>city|cat</c>), a zone filter uses <c>zone|cat</c>
    /// for the categories; grouping by city with a zone filter is refused.
    /// </summary>
    public async Task<KpiBreakdownResponse> BreakdownAsync(string? metric, DateOnly? from, DateOnly? to, string? groupBy, Guid? cityId, Guid? zoneId, Guid? rideCategoryId,
        Language lang, CancellationToken ct)
    {
        new Validator().Require(nameof(metric), metric, 60).Require(nameof(groupBy), groupBy, 20).ThrowIfInvalid();
        var definition = KpiCodes.ByCode.GetValueOrDefault(metric!.Trim())
                         ?? throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["metric"] = "unknown metric" });
        var (start, end) = ValidateRange(from, to);
        var group = QueryEnum.Parse<KpiGroupBy>(groupBy, nameof(groupBy))!.Value;
        await ValidateScopeAsync(cityId, zoneId, rideCategoryId, ct);
        new Validator().Rule(nameof(groupBy), !(group == KpiGroupBy.City && zoneId is not null), "city cannot be combined with a zone filter").ThrowIfInvalid();
        var zoneCities = group == KpiGroupBy.Zone && cityId is not null
            ? await db.Zones.AsNoTracking().Select(z => new { z.Id, z.CityId }).ToDictionaryAsync(z => z.Id.ToString(), z => z.CityId.ToString(), ct)
            : [];
        string? RowKey(string scopeKey)
        {
            var parts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in scopeKey.Split('|'))
            {
                var pair = part.Split(':', 2);
                if (pair.Length != 2) return null;
                parts[pair[0]] = pair[1];
            }

            var cat = parts.GetValueOrDefault("cat");
            var city = parts.GetValueOrDefault("city");
            var zone = parts.GetValueOrDefault("zone");
            var categoryFilter = rideCategoryId?.ToString();
            switch (group)
            {
                case KpiGroupBy.City:
                    if (city is null || zone is not null || (categoryFilter is null ? cat is not null : cat != categoryFilter)) return null;
                    return cityId is null || city == cityId.ToString() ? city : null;
                case KpiGroupBy.Zone:
                    if (zone is null || city is not null || (categoryFilter is null ? cat is not null : cat != categoryFilter)) return null;
                    if (zoneId is not null && zone != zoneId.ToString()) return null;
                    return cityId is null || zoneCities.GetValueOrDefault(zone) == cityId.ToString() ? zone : null;
                default:
                    if (cat is null || (categoryFilter is not null && cat != categoryFilter)) return null;
                    if (zoneId is not null) return zone == zoneId.ToString() && city is null ? cat : null;
                    if (cityId is not null) return city == cityId.ToString() && zone is null ? cat : null;
                    return city is null && zone is null ? cat : null;
            }
        }

        var rows = new List<KpiBreakdownRowDto>();
        if (definition.Dimensional)
        {
            Dictionary<string, KpiCell> byKey;
            if (definition.Aggregation == KpiAggregation.Distinct)
            {
                var computation = await calculator.ComputeAsync(start, end, _ => start, new HashSet<string> { definition.Code }, ct);
                byKey = computation.Scopes.Keys.Select(k => (Scope: k, Key: RowKey(k))).Where(x => x.Key is not null)
                    .ToDictionary(x => x.Key!, x => KpiCell.From(definition, computation.Get(start, x.Scope, definition.Code)));
            }
            else
            {
                var cells = await DailyCellsAsync(start, end, null, [definition], ct);
                byKey = cells.Select(c => (Cell: c, Key: RowKey(c.Scope))).Where(x => x.Key is not null)
                    .GroupBy(x => x.Key!).ToDictionary(g => g.Key, g => Aggregate(definition, g.Select(x => x.Cell).ToList()));
            }

            var labels = await LabelsAsync(group, lang, ct);
            rows = byKey.Select(kv => new KpiBreakdownRowDto(kv.Key, labels.GetValueOrDefault(kv.Key) ?? kv.Key, KpiMath.Display(definition, kv.Value.Value), kv.Value.Numerator, kv.Value.Denominator))
                .OrderByDescending(r => r.Value ?? decimal.MinValue).ThenBy(r => r.Label).ToList();
        }

        return new KpiBreakdownResponse(definition.Code, lang.Pick(definition.NameAr, definition.NameEn), definition.Unit, group, start, end, rows);
    }

    /// <summary><c>today</c> block of <c>/admin/dashboard/summary</c> (Riyadh day, computed live).</summary>
    public async Task<DashboardTodayDto> TodayAsync(CancellationToken ct)
    {
        var today = Formats.RiyadhDate(clock.UtcNow);
        var metrics = new HashSet<string> { KpiCodes.CompletedTrips, KpiCodes.Gmv, KpiCodes.ActiveDrivers, KpiCodes.AverageTimeToAssign };
        var computation = await calculator.ComputeAsync(today, today, d => d, metrics, ct);
        KpiCell Cell(string code) => KpiCell.From(KpiCodes.ByCode[code], computation.Get(today, KpiScope.All.Key, code));
        var avg = Cell(KpiCodes.AverageTimeToAssign).Value;
        return new DashboardTodayDto(
            (int)(Cell(KpiCodes.CompletedTrips).Value ?? 0),
            decimal.Round(Cell(KpiCodes.Gmv).Value ?? 0m, 2),
            (int)(Cell(KpiCodes.ActiveDrivers).Value ?? 0),
            await db.Drivers.CountAsync(d => d.IsOnline, ct),
            avg is null ? null : decimal.Round(avg.Value, 0),
            await db.SafetyCases.CountAsync(c => c.Status != SafetyCaseStatus.Resolved, ct),
            await db.SupportTickets.CountAsync(t => t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed, ct));
    }

    // ----- helpers -----

    public (DateOnly From, DateOnly To) ValidateRange(DateOnly? from, DateOnly? to)
    {
        new Validator().Require(nameof(from), from).Require(nameof(to), to)
            .Rule(nameof(to), from is null || to is null || to >= from, "must not be before from")
            .ThrowIfInvalid();
        ReportRange.EnsureWithin(from!.Value, to!.Value, options.Value.MaxRangeDays);
        return (from.Value, to.Value);
    }

    public async Task<KpiScope> ValidateScopeAsync(Guid? cityId, Guid? zoneId, Guid? rideCategoryId, CancellationToken ct)
    {
        var v = new Validator();
        if (cityId is { } c) v.Rule(nameof(cityId), await db.Cities.AsNoTracking().AnyAsync(x => x.Id == c, ct), "unknown city");
        if (zoneId is { } z)
        {
            var zoneCity = await db.Zones.AsNoTracking().Where(x => x.Id == z).Select(x => (Guid?)x.CityId).FirstOrDefaultAsync(ct);
            v.Rule(nameof(zoneId), zoneCity is not null, "unknown zone");
            v.Rule(nameof(zoneId), zoneCity is null || cityId is null || zoneCity == cityId, "the zone is not in the city");
        }

        if (rideCategoryId is { } r) v.Rule(nameof(rideCategoryId), await db.RideCategories.AsNoTracking().AnyAsync(x => x.Id == r, ct), "unknown ride category");
        v.ThrowIfInvalid();
        return KpiScope.For(cityId, zoneId, rideCategoryId);
    }

    private static List<KpiDefinition> ParseMetrics(string? metrics)
    {
        if (string.IsNullOrWhiteSpace(metrics))
        {
            return KpiCodes.All.ToList();
        }

        var codes = metrics.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
        var unknown = codes.Where(c => !KpiCodes.AllCodes.Contains(c)).ToList();
        if (unknown.Count > 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["metrics"] = "unknown metric: " + string.Join(',', unknown) });
        }

        return KpiCodes.All.Where(d => codes.Contains(d.Code)).ToList();
    }

    private static bool ApplicableTo(KpiDefinition definition, KpiScope scope) => definition.Dimensional || scope.Key == KpiScope.All.Key;

    private async Task<Dictionary<string, KpiCell>> AggregateRangeAsync(DateOnly from, DateOnly to, KpiScope scope, IReadOnlyList<KpiDefinition> definitions, CancellationToken ct)
    {
        var result = new Dictionary<string, KpiCell>(StringComparer.Ordinal);
        var applicable = definitions.Where(d => ApplicableTo(d, scope)).ToList();
        foreach (var d in definitions.Where(d => !ApplicableTo(d, scope)))
        {
            result[d.Code] = new KpiCell(null, null, null);
        }

        var distinct = applicable.Where(d => d.Aggregation == KpiAggregation.Distinct).ToList();
        if (distinct.Count > 0)
        {
            var computation = await calculator.ComputeAsync(from, to, _ => from, distinct.Select(d => d.Code).ToHashSet(), ct);
            foreach (var d in distinct)
            {
                result[d.Code] = KpiCell.From(d, computation.Get(from, scope.Key, d.Code));
            }
        }

        var additive = applicable.Where(d => d.Aggregation != KpiAggregation.Distinct).ToList();
        if (additive.Count > 0)
        {
            var cells = await DailyCellsAsync(from, to, [scope.Key], additive, ct);
            foreach (var d in additive)
            {
                result[d.Code] = Aggregate(d, cells.Where(c => c.Metric == d.Code).ToList());
            }
        }

        return result;
    }

    /// <summary>Σ values (sum) or Σ numerator ÷ Σ denominator (ratio / avg / a single day of a distinct ratio).</summary>
    private static KpiCell Aggregate(KpiDefinition definition, IReadOnlyList<DailyCell> cells)
    {
        if (definition.Aggregation == KpiAggregation.Sum)
        {
            var numerators = cells.Where(c => c.Numerator != null).ToList();
            return new KpiCell(cells.Sum(c => c.Value), numerators.Count > 0 ? numerators.Sum(c => c.Numerator!.Value) : null, null);
        }

        if (definition.Aggregation == KpiAggregation.Distinct && definition.Unit == KpiUnit.Count)
        {
            // Only reached for a daily series: one cell per day.
            return new KpiCell(cells.Sum(c => c.Value), null, null);
        }

        var numerator = cells.Sum(c => c.Numerator ?? 0m);
        var denominator = cells.Sum(c => c.Denominator ?? 0m);
        return new KpiCell(KpiMath.Ratio(definition, numerator, denominator), numerator, denominator);
    }

    /// <summary>
    /// Daily cells of <paramref name="definitions"/> for the scopes (null = every scope): snapshot rows for days that have one, live computation for the
    /// others (today and later, or days never snapshotted).
    /// </summary>
    private async Task<List<DailyCell>> DailyCellsAsync(DateOnly from, DateOnly to, IReadOnlyCollection<string>? scopes, IReadOnlyList<KpiDefinition> definitions, CancellationToken ct)
    {
        var codes = definitions.Select(d => d.Code).ToList();
        var today = Formats.RiyadhDate(clock.UtcNow);
        var lastStored = to < today ? to : today.AddDays(-1);
        var snapshotDays = from <= lastStored
            ? (await db.ReportSnapshots.AsNoTracking().Where(s => s.ScopeKey == ReportSnapshot.AllScope && s.SnapshotDate >= from && s.SnapshotDate <= lastStored)
                .Select(s => s.SnapshotDate).Distinct().ToListAsync(ct)).ToHashSet()
            : [];
        var cells = new List<DailyCell>();
        if (snapshotDays.Count > 0)
        {
            var query = db.ReportSnapshots.AsNoTracking().Where(s => s.SnapshotDate >= from && s.SnapshotDate <= lastStored && codes.Contains(s.MetricCode));
            if (scopes is not null)
            {
                query = query.Where(s => scopes.Contains(s.ScopeKey));
            }

            cells.AddRange((await query.Select(s => new { s.SnapshotDate, s.ScopeKey, s.MetricCode, s.Value, s.Numerator, s.Denominator }).ToListAsync(ct))
                .Where(s => snapshotDays.Contains(s.SnapshotDate))
                .Select(s => new DailyCell(s.SnapshotDate, s.ScopeKey, s.MetricCode, s.Value, s.Numerator, s.Denominator)));
        }

        var missing = new List<DateOnly>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (!snapshotDays.Contains(day))
            {
                missing.Add(day);
            }
        }

        if (missing.Count > 0)
        {
            var computation = await calculator.ComputeAsync(missing[0], missing[^1], d => d, codes.ToHashSet(), ct);
            var wanted = missing.ToHashSet();
            foreach (var ((day, scope), metrics) in computation.Cells)
            {
                if (!wanted.Contains(day) || (scopes is not null && !scopes.Contains(scope)))
                {
                    continue;
                }

                foreach (var (code, acc) in metrics.Where(m => codes.Contains(m.Key)))
                {
                    var cell = KpiCell.From(KpiCodes.ByCode[code], acc);
                    cells.Add(new DailyCell(day, scope, code, cell.Value ?? 0m, cell.Numerator, cell.Denominator));
                }
            }
        }

        return cells;
    }

    private async Task<Dictionary<string, string>> LabelsAsync(KpiGroupBy group, Language lang, CancellationToken ct) => group switch
    {
        KpiGroupBy.City => (await db.Cities.AsNoTracking().Select(c => new { c.Id, c.NameAr, c.NameEn }).ToListAsync(ct)).ToDictionary(c => c.Id.ToString(), c => lang.Pick(c.NameAr, c.NameEn)),
        KpiGroupBy.Zone => (await db.Zones.AsNoTracking().Select(z => new { z.Id, z.NameAr, z.NameEn }).ToListAsync(ct)).ToDictionary(z => z.Id.ToString(), z => lang.Pick(z.NameAr, z.NameEn)),
        _ => (await db.RideCategories.AsNoTracking().Select(r => new { r.Id, r.NameAr, r.NameEn }).ToListAsync(ct)).ToDictionary(r => r.Id.ToString(), r => lang.Pick(r.NameAr, r.NameEn)),
    };
}
