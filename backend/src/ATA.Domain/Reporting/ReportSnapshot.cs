namespace ATA.Domain.Reporting;

/// <summary>
/// Row of <c>report_snapshots</c> (doc 12 §F20.1): one metric of one Riyadh day for one scope (<c>all</c>, <c>city:{id}</c>, <c>zone:{id}</c>, <c>cat:{id}</c>,
/// <c>city:{id}|cat:{id}</c>, <c>zone:{id}|cat:{id}</c>). <c>ratio</c>/<c>avg</c> metrics keep their numerator and denominator so that several days aggregate as
/// Σ numerator ÷ Σ denominator.
/// </summary>
public class ReportSnapshot
{
    public const string AllScope = "all";

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateOnly SnapshotDate { get; set; }
    public required string ScopeKey { get; set; }
    public Guid? CityId { get; set; }
    public Guid? ZoneId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public required string MetricCode { get; set; }
    public decimal Value { get; set; }
    public decimal? Numerator { get; set; }
    public decimal? Denominator { get; set; }
    public DateTime ComputedAt { get; set; }
}
