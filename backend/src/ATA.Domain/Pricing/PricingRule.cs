using ATA.Domain.Common;

namespace ATA.Domain.Pricing;

/// <summary>Tariff for one ride category, either city-wide (<see cref="ZoneId"/> null) or for one zone (<c>pricing_rules</c>).</summary>
public class PricingRule : AuditableEntity
{
    public Guid RideCategoryId { get; set; }
    public Guid? ZoneId { get; set; }
    public required string Name { get; set; }
    public decimal BaseFare { get; set; }
    public decimal PerKm { get; set; }
    public decimal PerMinute { get; set; }
    public decimal BookingFee { get; set; }
    public decimal ServiceFeePercent { get; set; }
    public decimal MinFare { get; set; }
    public decimal WaitingPerMinute { get; set; }
    public int FreeWaitingMinutes { get; set; } = 3;
    /// <summary>Charged by the cancellation policy (F14).</summary>
    public decimal CancellationFee { get; set; }
    public decimal DriverSharePercent { get; set; } = 80m;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>Highest priority wins among the rules matching the same category and zone.</summary>
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PricingTimeMultiplier> TimeMultipliers { get; set; } = [];

    public bool IsEffectiveAt(DateTime utc) => IsActive && EffectiveFrom <= utc && (EffectiveTo is null || EffectiveTo > utc);

    /// <summary>Highest multiplier whose window contains the local pickup time, or 1.</summary>
    public (decimal Multiplier, string? Label) TimeMultiplierAt(DateTime localTime)
    {
        var best = TimeMultipliers.Where(m => m.AppliesAt(localTime)).OrderByDescending(m => m.Multiplier).FirstOrDefault();
        return best is null ? (1m, null) : (best.Multiplier, best.Label);
    }
}

/// <summary>Time-of-day multiplier (<c>pricing_time_multipliers</c>); <c>from &gt; to</c> spans midnight.</summary>
public class PricingTimeMultiplier : Entity
{
    public Guid PricingRuleId { get; set; }
    /// <summary>0 = Sunday … 6 = Saturday; null = every day.</summary>
    public byte? DayOfWeek { get; set; }
    public TimeOnly FromTime { get; set; }
    public TimeOnly ToTime { get; set; }
    public decimal Multiplier { get; set; } = 1m;
    public required string Label { get; set; }

    public bool AppliesAt(DateTime localTime)
    {
        var time = TimeOnly.FromDateTime(localTime);
        var inWindow = FromTime <= ToTime ? time >= FromTime && time < ToTime : time >= FromTime || time < ToTime;
        if (!inWindow)
        {
            return false;
        }

        if (DayOfWeek is null)
        {
            return true;
        }

        // For an overnight window the part after midnight belongs to the day the window started on.
        var day = (int)localTime.DayOfWeek;
        if (FromTime > ToTime && time < ToTime)
        {
            day = (day + 6) % 7;
        }

        return day == DayOfWeek;
    }
}
