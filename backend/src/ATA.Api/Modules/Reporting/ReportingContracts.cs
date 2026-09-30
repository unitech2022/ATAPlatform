namespace ATA.Api.Modules.Reporting;

/// <summary><c>Reports:*</c> keys (doc 12 §F20.8).</summary>
public sealed class ReportsOptions
{
    public const string Section = "Reports";

    public int MaxRangeDays { get; set; } = 366;
    public int MaxExportRows { get; set; } = 100000;
    /// <summary>Days before "yesterday" recomputed by every <c>ReportSnapshotJob</c> run (late ratings, refunds, cancellations).</summary>
    public int RecomputeTrailingDays { get; set; } = 3;
    /// <summary>Riyadh local time of the daily snapshot (01:30).</summary>
    public int SnapshotHourLocal { get; set; } = 1;
    public int SnapshotMinuteLocal { get; set; } = 30;
    /// <summary><c>false</c> in tests (they call <c>RunOnceAsync</c>).</summary>
    public bool JobsEnabled { get; set; } = true;
}

public enum KpiUnit { Count, Percent, Ratio, Seconds, Sar, Hours, Rating }

/// <summary>How days combine (doc 12 §F20.6): Σ values, Σ numerator ÷ Σ denominator (ratio / avg), or computed directly for the range (distinct).</summary>
public enum KpiAggregation { Sum, Ratio, Avg, Distinct }

public enum KpiGranularity { Day, Week, Month }

public enum KpiGroupBy { City, Zone, Category }

public sealed record KpiFiltersDto(Guid? CityId, Guid? ZoneId, Guid? RideCategoryId);

public sealed record KpiMetricDto(
    string Code, string Name, KpiUnit Unit, KpiAggregation Aggregation, string Group, decimal? Value, decimal? PreviousValue, decimal? ChangePercent,
    decimal? Numerator, decimal? Denominator);

public sealed record KpisResponse(DateOnly From, DateOnly To, KpiFiltersDto Filters, string? Compare, DateOnly? PreviousFrom, DateOnly? PreviousTo, IReadOnlyList<KpiMetricDto> Metrics);

public sealed record KpiPointDto(DateOnly PeriodStart, decimal? Value, decimal? Numerator, decimal? Denominator);

public sealed record KpiSeriesResponse(string Code, string Name, KpiUnit Unit, KpiGranularity Granularity, DateOnly From, DateOnly To, IReadOnlyList<KpiPointDto> Points);

public sealed record KpiBreakdownRowDto(string Key, string Label, decimal? Value, decimal? Numerator, decimal? Denominator);

public sealed record KpiBreakdownResponse(string Metric, string Name, KpiUnit Unit, KpiGroupBy GroupBy, DateOnly From, DateOnly To, IReadOnlyList<KpiBreakdownRowDto> Rows);

public sealed record SnapshotRebuildRequest(DateOnly? From, DateOnly? To);

public sealed record SnapshotRebuildResponse(DateOnly From, DateOnly To, int Days, int Rows);

public sealed record KpiDefinitionDto(string Code, string Name, string NameAr, string NameEn, KpiUnit Unit, KpiAggregation Aggregation, string Group, bool Dimensional);

public sealed record DashboardTodayDto(int CompletedTrips, decimal Gmv, int ActiveDrivers, int OnlineDrivers, decimal? AvgTimeToAssignSeconds, int OpenSafetyCases, int OpenTickets);
