using ATA.Domain.Common;

namespace ATA.Domain.Pricing;

/// <summary>Demand tiers and their fare multipliers (<c>demand_levels</c>).</summary>
public class DemandLevel : Entity
{
    public const string Normal = "normal";
    public const string Moderate = "moderate";
    public const string High = "high";
    public const string VeryHigh = "very_high";

    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public decimal Multiplier { get; set; } = 1m;
    public required string Color { get; set; }
    public int SortOrder { get; set; }
}

public static class DemandMetrics
{
    public const string RequestsPerDriver = "requests_per_driver";
}

/// <summary>Thresholds that map the demand metric to a level (<c>demand_rules</c>); the most specific zone/category rule wins.</summary>
public class DemandRule : AuditableEntity
{
    public Guid? ZoneId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public string Metric { get; set; } = DemandMetrics.RequestsPerDriver;
    public int WindowMinutes { get; set; } = 10;
    public decimal ThresholdModerate { get; set; }
    public decimal ThresholdHigh { get; set; }
    public decimal ThresholdVeryHigh { get; set; }
    public bool IsActive { get; set; } = true;

    public string LevelFor(decimal ratio) => ratio switch
    {
        _ when ratio >= ThresholdVeryHigh => DemandLevel.VeryHigh,
        _ when ratio >= ThresholdHigh => DemandLevel.High,
        _ when ratio >= ThresholdModerate => DemandLevel.Moderate,
        _ => DemandLevel.Normal,
    };
}

/// <summary>Manual demand level forced by an admin for a time window (<c>demand_overrides</c>).</summary>
public class DemandOverride : Entity
{
    public Guid ZoneId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public Guid DemandLevelId { get; set; }
    public required string Reason { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public bool IsActiveAt(DateTime utc) => StartsAt <= utc && EndsAt > utc;
}

/// <summary>One computed demand reading per zone (and category when a rule targets one) (<c>demand_snapshots</c>).</summary>
public class DemandSnapshot : Entity
{
    public Guid ZoneId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public DateTime ComputedAt { get; set; }
    public int RequestsCount { get; set; }
    public int OnlineDrivers { get; set; }
    public decimal Ratio { get; set; }
    public required string DemandLevelCode { get; set; }
}
