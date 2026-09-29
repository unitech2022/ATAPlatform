using System.Text.Json;
using ATA.Domain.Common;
using ATA.Domain.Drivers;

namespace ATA.Domain.Incentives;

public enum TierChangeReason { WeeklyRecalc, Admin }

public enum IncentiveType { Daily, Weekly, ZoneQuest, OneTime }

public enum IncentiveProgressStatus { InProgress, Achieved, Paid, Expired, Voided }

/// <summary>Row of <c>driver_tier_rules</c> (doc 10 §F15.7): the thresholds and benefits of one tier.</summary>
public class DriverTierRule : AuditableEntity
{
    public DriverTier Tier { get; set; }
    public int MinCompletedTrips { get; set; }
    public decimal MinRatingAvg { get; set; }
    public decimal MinAcceptanceRate { get; set; }
    public decimal MaxCancellationRate { get; set; }
    public decimal CommissionDiscountPercent { get; set; }
    /// <summary>The F9 <c>norm_tier</c> value of this tier (0..1).</summary>
    public decimal MatchingNorm { get; set; }
    public string? BenefitsAr { get; set; }
    public string? BenefitsEn { get; set; }
    public int SortOrder { get; set; }

    /// <summary>Every condition is inclusive (a value exactly on the threshold qualifies).</summary>
    public bool IsMetBy(TierMetrics m) =>
        m.CompletedTrips >= MinCompletedTrips && m.RatingAvg >= MinRatingAvg && m.AcceptanceRate >= MinAcceptanceRate && m.CancellationRate <= MaxCancellationRate;
}

/// <summary>Inputs of the weekly tier recalculation.</summary>
public sealed record TierMetrics(int CompletedTrips, decimal RatingAvg, decimal AcceptanceRate, decimal CancellationRate);

/// <summary>Row of <c>driver_tier_history</c>. <see cref="Metrics"/> is the JSON of <see cref="TierMetrics"/> (null for manual changes).</summary>
public class DriverTierHistory : Entity
{
    public Guid DriverId { get; set; }
    public DriverTier? FromTier { get; set; }
    public DriverTier ToTier { get; set; }
    public string? Metrics { get; set; }
    public TierChangeReason Reason { get; set; }
    /// <summary>Admin reason of a manual change.</summary>
    public string? Note { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime ComputedAt { get; set; }
}

/// <summary>
/// Row of <c>driver_incentives</c> (doc 10 §F15.8). JSON arrays: <see cref="ZoneIds"/>, <see cref="RideCategoryIds"/>, <see cref="DaysOfWeek"/>
/// (0 = Sunday … 6 = Saturday). <see cref="DailyFrom"/>/<see cref="DailyTo"/> are Riyadh local times (the window may span midnight).
/// </summary>
public class DriverIncentive : AuditableEntity
{
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public IncentiveType Type { get; set; }
    public Guid CityId { get; set; }
    public string? ZoneIds { get; set; }
    public string? RideCategoryIds { get; set; }
    public int TargetTrips { get; set; }
    public decimal RewardAmount { get; set; }
    public decimal? MinTripFare { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string? DaysOfWeek { get; set; }
    public TimeOnly? DailyFrom { get; set; }
    public TimeOnly? DailyTo { get; set; }
    public DriverTier? MinTier { get; set; }
    public decimal? MinRating { get; set; }
    public bool RequiresOptIn { get; set; }
    public int? MaxParticipants { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public bool NotifyOnPublish { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>When <c>incentive.new</c> was sent (so an update does not notify again).</summary>
    public DateTime? PublishedAt { get; set; }
    public Guid? CreatedBy { get; set; }

    public bool IsRunningAt(DateTime utc) => IsActive && StartsAt <= utc && utc < EndsAt;

    public bool IsRecurring => Type is IncentiveType.Daily or IncentiveType.Weekly;

    /// <summary>
    /// The period containing <paramref name="utc"/> as UTC bounds, clipped to <c>[starts_at, ends_at)</c>: a local day (daily), a local Sunday–Saturday
    /// week (weekly), or the whole campaign (zone quest / one time). <c>null</c> outside the campaign.
    /// </summary>
    public (DateTime Start, DateTime End)? PeriodAt(DateTime utc, int utcOffsetMinutes)
    {
        if (utc < StartsAt || utc >= EndsAt)
        {
            return null;
        }

        if (!IsRecurring)
        {
            return (StartsAt, EndsAt);
        }

        var localDate = DateOnly.FromDateTime(utc.AddMinutes(utcOffsetMinutes));
        var first = Type == IncentiveType.Weekly ? localDate.AddDays(-(int)localDate.DayOfWeek) : localDate;
        var days = Type == IncentiveType.Weekly ? 7 : 1;
        var start = DateTime.SpecifyKind(first.ToDateTime(TimeOnly.MinValue).AddMinutes(-utcOffsetMinutes), DateTimeKind.Utc);
        var end = start.AddDays(days);
        return (start < StartsAt ? StartsAt : start, end > EndsAt ? EndsAt : end);
    }

    /// <summary>Whether <paramref name="utc"/> falls on one of <c>days_of_week</c> and inside <c>daily_from..daily_to</c> (local; minutes inclusive).</summary>
    public bool IsInWindow(DateTime utc, int utcOffsetMinutes)
    {
        var local = utc.AddMinutes(utcOffsetMinutes);
        var days = ParseDays(DaysOfWeek);
        if (days is not null && !days.Contains((int)local.DayOfWeek))
        {
            return false;
        }

        if (DailyFrom is not { } from || DailyTo is not { } to)
        {
            return true;
        }

        var tod = TimeOnly.FromDateTime(local);
        var minute = new TimeOnly(tod.Hour, tod.Minute);
        return from <= to ? minute >= from && minute <= to : minute >= from || minute <= to;
    }

    public static IReadOnlyList<int>? ParseDays(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<int>>(json);
}

/// <summary>Row of <c>driver_incentive_progress</c>: one per (incentive, driver, period).</summary>
public class DriverIncentiveProgress : AuditableEntity
{
    public Guid IncentiveId { get; set; }
    public Guid DriverId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime? OptedInAt { get; set; }
    public int CompletedTrips { get; set; }
    public IncentiveProgressStatus Status { get; set; } = IncentiveProgressStatus.InProgress;
    public DateTime? AchievedAt { get; set; }
    public decimal? IncentiveMultiplier { get; set; }
    /// <summary>The reward actually paid (after the F14 reliability multiplier).</summary>
    public decimal? RewardAmount { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? WalletTransactionId { get; set; }
    public string? VoidedReason { get; set; }

    public bool IsOpen => Status is IncentiveProgressStatus.InProgress or IncentiveProgressStatus.Achieved;
}

/// <summary>Row of <c>driver_incentive_trips</c>: a trip counted towards a progress row (once).</summary>
public class DriverIncentiveTrip : Entity
{
    public Guid ProgressId { get; set; }
    public Guid TripId { get; set; }
    public DateTime CountedAt { get; set; }
}

/// <summary>Tier benefits (doc 10 §F15.7).</summary>
public static class TierMath
{
    /// <summary>The F9 <c>norm_tier</c> used when no <c>driver_tier_rules</c> row exists.</summary>
    public static decimal DefaultNorm(DriverTier tier) => tier switch { DriverTier.Platinum => 1m, DriverTier.Gold => 0.75m, DriverTier.Silver => 0.5m, _ => 0.25m };

    /// <summary>
    /// The commission discount is a share of the commission, not percentage points:
    /// <c>effective = share + (100 − share) × discount / 100</c> (80 % with a 10 % discount → 82 %).
    /// </summary>
    public static decimal EffectiveSharePercent(decimal driverSharePercent, decimal commissionDiscountPercent) =>
        driverSharePercent + (100m - driverSharePercent) * commissionDiscountPercent / 100m;
}
