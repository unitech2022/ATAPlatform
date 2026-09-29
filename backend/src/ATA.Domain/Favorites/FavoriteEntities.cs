using ATA.Domain.Common;

namespace ATA.Domain.Favorites;

/// <summary>Row of <c>favorite_drivers</c> (doc 10 §F16.1): a driver a passenger rode with and saved. UNIQUE(passenger_id, driver_id).</summary>
public class FavoriteDriver : Entity
{
    public Guid PassengerId { get; set; }
    public Guid DriverId { get; set; }
    /// <summary>The completed trip the favourite was added from (the latest shared trip when added by driver id).</summary>
    public Guid? SourceTripId { get; set; }
}

/// <summary>
/// Row of <c>favorite_driver_discount_rules</c> (doc 10 §F16.1). JSON arrays: <see cref="RideCategoryIds"/>, <see cref="ZoneIds"/> (pickup zone) and
/// <see cref="BookingTypes"/> (<c>["now","scheduled"]</c>); null = no restriction. The active rule with the highest <see cref="Priority"/> that
/// matches the trip is pinned when the favourite driver accepts.
/// </summary>
public class FavoriteDriverDiscountRule : AuditableEntity
{
    public const int NameMaxLength = 120;
    public const decimal MinPercent = 1m;
    public const decimal MaxPercent = 50m;

    public required string Name { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public decimal? MinFare { get; set; }
    public bool StackableWithPromotions { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? RideCategoryIds { get; set; }
    public string? ZoneIds { get; set; }
    public string? BookingTypes { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }

    public bool IsWithinValidity(DateTime now) => ValidFrom <= now && (ValidTo is null || now <= ValidTo);

    /// <summary>The discount on <paramref name="baseFare"/> (the F10 fare before discount and rounding): <c>base × percent / 100</c> capped by <c>max_discount_amount</c>.</summary>
    public decimal AmountFor(decimal baseFare)
    {
        var amount = decimal.Round(baseFare * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        return Math.Max(0m, Math.Min(amount, MaxDiscountAmount));
    }
}
