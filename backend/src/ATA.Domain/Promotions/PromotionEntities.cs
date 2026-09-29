using ATA.Domain.Common;

namespace ATA.Domain.Promotions;

public enum PromotionType { Percent, Fixed, FreeBookingFee }

public enum RedemptionStatus { Reserved, Applied, Released }

public enum RedemptionReleaseReason { TripCancelled, NoDrivers, NotStacked, PaymentFailed, NotEligibleAtCompletion, Admin }

/// <summary>
/// Row of <c>promotions</c> (doc 10 §F15.4). JSON arrays: <see cref="RideCategoryIds"/>, <see cref="ZoneIds"/> (pickup zone),
/// <see cref="PaymentMethods"/>, <see cref="BookingTypes"/> (null = no restriction). <see cref="UsageCount"/> counts reserved + applied
/// redemptions; a deleted code is only deactivated so it is never reused.
/// </summary>
public class Promotion : AuditableEntity
{
    public const int CodeMinLength = 4;
    public const int CodeMaxLength = 20;

    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public PromotionType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MaxDiscount { get; set; }
    public decimal? MinFare { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public int? TotalUsageLimit { get; set; }
    public int PerUserLimit { get; set; } = 1;
    public int UsageCount { get; set; }
    public decimal? BudgetAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public bool FirstTripOnly { get; set; }
    public bool NewUsersOnly { get; set; }
    public int NewUserDays { get; set; } = 30;
    public Guid? CityId { get; set; }
    public string? RideCategoryIds { get; set; }
    public string? ZoneIds { get; set; }
    public string? PaymentMethods { get; set; }
    public string? BookingTypes { get; set; }
    public bool IsStackable { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }

    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static bool IsValidCode(string code) =>
        code.Length is >= CodeMinLength and <= CodeMaxLength && code.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9');

    public bool IsWithinValidity(DateTime now) => ValidFrom <= now && now <= ValidTo;

    /// <summary>
    /// The discount on <paramref name="baseFare"/> (the F10 fare before discount and rounding): percent → <c>base × value / 100</c> capped by
    /// <c>max_discount</c>; fixed → <c>min(value, base)</c>; free booking fee → the booking fee.
    /// </summary>
    public decimal AmountFor(decimal baseFare, decimal bookingFee)
    {
        var amount = Type switch
        {
            PromotionType.Percent => decimal.Round(baseFare * Value / 100m, 2, MidpointRounding.AwayFromZero),
            PromotionType.Fixed => Math.Min(Value, baseFare),
            _ => Math.Min(bookingFee, baseFare),
        };
        if (MaxDiscount is { } cap && Type == PromotionType.Percent)
        {
            amount = Math.Min(amount, cap);
        }

        return Math.Max(0m, amount);
    }
}

/// <summary>Row of <c>promotion_redemptions</c>: one code per trip, reserved at request, applied at completion or released.</summary>
public class PromotionRedemption : Entity
{
    public Guid PromotionId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid TripId { get; set; }
    public RedemptionStatus Status { get; set; } = RedemptionStatus.Reserved;
    public decimal? ReservedAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public RedemptionReleaseReason? ReleaseReason { get; set; }
}
