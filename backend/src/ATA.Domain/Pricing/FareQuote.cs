using ATA.Domain.Common;

namespace ATA.Domain.Pricing;

/// <summary>
/// A priced offer for one ride category (<c>fare_quotes</c>). One <c>POST /pricing/quote</c> call writes one row per category
/// sharing <see cref="GroupId"/>, so a trip request can reference the group with any category's <c>quoteId</c>.
/// </summary>
public class FareQuote : Entity
{
    public Guid GroupId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid RideCategoryId { get; set; }
    public Guid? PickupZoneId { get; set; }
    public Guid? DropoffZoneId { get; set; }
    public int DistanceM { get; set; }
    public int DurationS { get; set; }
    public required string Breakdown { get; set; }
    public required string DemandLevelCode { get; set; }
    public decimal Total { get; set; }
    /// <summary>F15: the fare before discounts and rounding (the discount engine's <c>base</c>); 0 on quotes stored before F15.</summary>
    public decimal BaseAmount { get; set; }
    public decimal DriverNetEarnings { get; set; }
    public decimal DriverSharePercent { get; set; }
    public decimal OfferMin { get; set; }
    public decimal OfferMax { get; set; }
    public Guid? PricingRuleId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? UsedTripId { get; set; }

    public bool IsUsableAt(DateTime utc) => UsedTripId is null && ExpiresAt > utc;
}
