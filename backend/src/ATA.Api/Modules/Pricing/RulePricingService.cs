using ATA.Api.Modules.Trips;
using ATA.Domain.Catalog;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Pricing;

/// <summary>
/// The F10 pricing engine. Picks the effective rule with the highest priority for the category and pickup zone (then the city-wide
/// rule), applies the time-of-day multiplier and the zone's current demand level (capped by <c>surge_cap</c>), and rounds the total to
/// 0.5 SAR:
/// <code>
/// subtotal  = max(base + per_km × km + per_minute × min + waiting_per_minute × waitMin, min_fare)
/// fare      = subtotal × timeMult × demand + booking_fee;  fare += fare × service_fee% ; fare -= discount
/// total     = round(fare, 0.5);  driverNet = subtotal × timeMult × demand × driver_share%
/// </code>
/// Without a matching rule the F8 <see cref="FlatPricing"/> formula is used.
/// </summary>
public sealed class RulePricingService(AtaDbContext db, ZoneResolver zones, DemandService demand, FlatPricing fallback, IOptions<PricingOptions> options) : IPricingService
{
    private readonly PricingOptions _options = options.Value;

    public RouteEstimate EstimateRoute(GeoPoint pickup, IReadOnlyList<GeoPoint> stops, GeoPoint dropoff) => fallback.EstimateRoute(pickup, stops, dropoff);

    public int EtaSeconds(double distanceMeters) => fallback.EtaSeconds(distanceMeters);

    public async Task<FareCalculation> CalculateAsync(FareRequest request, CancellationToken ct)
    {
        var pickupZone = await zones.ResolveAsync(request.Pickup.Lat, request.Pickup.Lng, request.At, ct);
        var dropoffZone = request.Dropoff is { } dropoff ? await zones.ResolveAsync(dropoff.Lat, dropoff.Lng, request.At, ct) : null;
        var rule = await SelectRuleAsync(request.Category.Id, pickupZone?.Id, request.At, ct);
        if (rule is null)
        {
            return fallback.Calculate(request) with { PickupZone = pickupZone, DropoffZone = dropoffZone };
        }

        var reading = request.LockedDemand ?? await demand.ReadAsync(pickupZone, request.Category.Id, request.At, ct);
        var (timeMultiplier, label) = rule.TimeMultiplierAt(PricingMath.ToLocal(request.At, _options));

        var km = request.DistanceMeters / 1000m;
        var minutes = request.DurationSeconds / 60m;
        var waitingMinutes = Math.Max(0, request.WaitingSeconds) / 60m;
        var distanceFare = PricingMath.Round2(rule.PerKm * km);
        var timeFare = PricingMath.Round2(rule.PerMinute * minutes);
        var waitingFare = PricingMath.Round2(rule.WaitingPerMinute * waitingMinutes);
        var subtotal = rule.BaseFare + distanceFare + timeFare + waitingFare;
        var minApplied = subtotal < rule.MinFare;
        subtotal = Math.Max(subtotal, rule.MinFare);

        var core = subtotal * timeMultiplier * reading.Multiplier;
        var fare = core + rule.BookingFee;
        var serviceFee = PricingMath.Round2(fare * rule.ServiceFeePercent / 100m);
        fare += serviceFee;
        // Discounts (F15 promo codes) are applied on top of this calculation by the discount engine; the driver share is computed before them.
        const decimal discount = 0m;

        var total = PricingMath.RoundToHalf(fare);
        var driverNet = PricingMath.Round2(core * rule.DriverSharePercent / 100m);
        var breakdown = new FareBreakdown(rule.BaseFare, distanceFare, timeFare, waitingFare, minApplied, timeMultiplier, label, reading.Multiplier, rule.BookingFee, serviceFee, discount);
        return new FareCalculation(
            total, driverNet, rule.DriverSharePercent,
            PricingMath.RoundToHalf(total * _options.OfferMinPercent / 100m), PricingMath.RoundToHalf(total * _options.OfferMaxPercent / 100m),
            breakdown, reading, pickupZone, dropoffZone, rule.Id, FareCalculation.SourceRule)
        {
            Base = fare,
            ShareBase = core,
        };
    }

    public async Task<int> FreeWaitingMinutesAsync(RideCategory category, GeoPoint pickup, DateTime at, CancellationToken ct)
    {
        var zone = await zones.ResolveAsync(pickup.Lat, pickup.Lng, at, ct);
        var rule = await SelectRuleAsync(category.Id, zone?.Id, at, ct);
        return rule?.FreeWaitingMinutes ?? await fallback.FreeWaitingMinutesAsync(category, pickup, at, ct);
    }

    /// <summary>Active rules effective at <paramref name="at"/>: the zone's rules first (highest priority), then the city-wide ones.</summary>
    public async Task<PricingRule?> SelectRuleAsync(Guid rideCategoryId, Guid? zoneId, DateTime at, CancellationToken ct)
    {
        var rules = await db.PricingRules.AsNoTracking().Include(r => r.TimeMultipliers)
            .Where(r => r.RideCategoryId == rideCategoryId && r.IsActive && (r.ZoneId == null || r.ZoneId == zoneId)
                        && r.EffectiveFrom <= at && (r.EffectiveTo == null || r.EffectiveTo > at))
            .ToListAsync(ct);
        return rules
            .OrderByDescending(r => r.ZoneId != null)
            .ThenByDescending(r => r.Priority)
            .ThenByDescending(r => r.EffectiveFrom)
            .FirstOrDefault();
    }
}
