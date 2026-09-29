using ATA.Domain.Pricing;

namespace ATA.Api.Modules.Pricing;

public sealed class PricingOptions
{
    public const string Section = "Pricing";
    /// <summary>Lower bound of "offer your price" as a percentage of the quoted total.</summary>
    public decimal OfferMinPercent { get; set; } = 70m;
    /// <summary>Upper bound of "offer your price" as a percentage of the quoted total.</summary>
    public decimal OfferMaxPercent { get; set; } = 130m;
    public int QuoteExpiryMinutes { get; set; } = 5;
    /// <summary>Offset applied to UTC for time multipliers and operating hours (Riyadh = +180).</summary>
    public int UtcOffsetMinutes { get; set; } = 180;
}

public sealed class DemandOptions
{
    public const string Section = "Demand";
    /// <summary>Runs the demand loop (<c>false</c> in tests, which call one pass explicitly).</summary>
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    /// <summary>A snapshot older than this no longer drives the level (2 × the default 10-minute window).</summary>
    public int SnapshotMaxAgeMinutes { get; set; } = 20;
    /// <summary>Snapshots older than this are pruned by the demand loop.</summary>
    public int SnapshotRetentionHours { get; set; } = 48;
}

/// <summary>Where a demand level came from.</summary>
public enum DemandSource { Default, Snapshot, Override }

/// <summary>Cached, parsed view of a <see cref="Zone"/> used by the resolver, the pricing engine and the matcher.</summary>
public sealed record ZoneSnapshot(
    Guid Id,
    Guid CityId,
    string Code,
    string NameAr,
    string NameEn,
    int Priority,
    bool IsCityDefault,
    decimal CenterLat,
    decimal CenterLng,
    GeoPolygon Ring,
    OperatingSchedule Schedule,
    IReadOnlyDictionary<Guid, ZoneCategoryRule> CategorySettings)
{
    public bool AllowsCategory(Guid rideCategoryId) => !CategorySettings.TryGetValue(rideCategoryId, out var s) || s.IsEnabled;

    public decimal SurgeCapFor(Guid? rideCategoryId) =>
        rideCategoryId is { } id && CategorySettings.TryGetValue(id, out var s) ? s.SurgeCap : ZoneCategorySetting.DefaultSurgeCap;
}

public sealed record ZoneCategoryRule(bool IsEnabled, decimal SurgeCap);

/// <summary>The demand level in force for a zone/category, with the multiplier already capped by the zone's <c>surge_cap</c>.</summary>
public sealed record DemandReading(string Code, string NameAr, string NameEn, decimal Multiplier, decimal LevelMultiplier, string Color, DemandSource Source, DateTime? ComputedAt, decimal? Ratio)
{
    public static DemandReading Neutral(DemandLevel? normal = null) =>
        new(DemandLevel.Normal, normal?.NameAr ?? "طبيعي", normal?.NameEn ?? "Normal", 1m, 1m, normal?.Color ?? "#19B7A5", DemandSource.Default, null, null);
}

public sealed record FareBreakdown(
    decimal BaseFare,
    decimal DistanceFare,
    decimal TimeFare,
    decimal WaitingFare,
    bool MinFareApplied,
    decimal TimeMultiplier,
    string? TimeMultiplierLabel,
    decimal DemandMultiplier,
    decimal BookingFee,
    decimal ServiceFee,
    decimal Discount);

/// <summary>A priced fare: the rounded total, the driver's net share, the "offer your price" bounds and how the number was built.</summary>
public sealed record FareCalculation(
    decimal Total,
    decimal DriverNetEarnings,
    decimal DriverSharePercent,
    decimal OfferMin,
    decimal OfferMax,
    FareBreakdown Breakdown,
    DemandReading Demand,
    ZoneSnapshot? PickupZone,
    ZoneSnapshot? DropoffZone,
    Guid? PricingRuleId,
    string Source)
{
    public const string SourceRule = "pricing_rule";
    public const string SourceFallback = "flat_pricing";

    /// <summary>F15: the fare before discounts and rounding (the discount engine's <c>base</c>).</summary>
    public decimal Base { get; init; }

    /// <summary>F15: the amount the driver share applies to (the F10 core <c>subtotal × timeMult × demand</c>; the whole fare for the flat fallback).</summary>
    public decimal ShareBase { get; init; }
}

public static class PricingMath
{
    /// <summary>Rounds to the nearest 0.5 SAR (midpoints away from zero).</summary>
    public static decimal RoundToHalf(decimal value) => decimal.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;

    public static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static DateTime ToLocal(DateTime utc, PricingOptions options) => DateTime.SpecifyKind(utc.AddMinutes(options.UtcOffsetMinutes), DateTimeKind.Unspecified);
}
