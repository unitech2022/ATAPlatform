using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Common;
using ATA.Domain.Favorites;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Favorites;

/// <summary>The pinned rule of a completing trip with the discount recomputed on the final base fare.</summary>
public sealed record FavoriteCompletion(FavoriteDriverDiscountRule Rule, decimal Amount)
{
    public DiscountCandidate Candidate => new(DiscountSources.FavoriteDriver, null, Amount, Rule.StackableWithPromotions);
}

/// <summary>
/// The favourite slot of the discount engine (doc 10 §1 and §F16.2): rule selection (highest <c>priority</c> among the active, currently valid rules that match the category,
/// pickup zone, booking type and <c>min_fare</c>; equal priorities are broken by the larger <c>discount_percent</c>, then the most recently created rule), the quote's conditional discount, pinning the rule when the favourite driver accepts and the recomputation at completion.
/// </summary>
public sealed class FavoriteDiscountService(AtaDbContext db, IClock clock, ZoneResolver zones, PromotionService promotions, IDiscountEngine engine)
{
    /// <summary>Whether <paramref name="rule"/> applies (restriction lists are checked only when set; <paramref name="baseFare"/> <c>null</c> skips <c>min_fare</c>).</summary>
    public static bool Matches(FavoriteDriverDiscountRule rule, Guid rideCategoryId, Guid? pickupZoneId, BookingType bookingType, decimal? baseFare)
    {
        if (JsonLists.Parse<Guid>(rule.RideCategoryIds) is { Count: > 0 } categories && !categories.Contains(rideCategoryId)) return false;
        if (JsonLists.Parse<Guid>(rule.ZoneIds) is { Count: > 0 } ruleZones && (pickupZoneId is not { } zone || !ruleZones.Contains(zone))) return false;
        if (JsonLists.Parse<BookingType>(rule.BookingTypes) is { Count: > 0 } bookings && !bookings.Contains(bookingType)) return false;
        if (rule.MinFare is { } minFare && baseFare is { } fare && fare < minFare) return false;
        return true;
    }

    public async Task<FavoriteDriverDiscountRule?> ResolveRuleAsync(Guid rideCategoryId, Guid? pickupZoneId, BookingType bookingType, decimal? baseFare, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var rules = await db.FavoriteDriverDiscountRules.AsNoTracking().Where(r => r.IsActive && r.ValidFrom <= now && (r.ValidTo == null || r.ValidTo >= now)).ToListAsync(ct);
        return rules
            .Where(r => Matches(r, rideCategoryId, pickupZoneId, bookingType, baseFare))
            .OrderByDescending(r => r.Priority).ThenByDescending(r => r.DiscountPercent).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .FirstOrDefault();
    }

    /// <summary>The discount a quote shows "assuming the favourite accepts" (<c>favoriteDiscountConditional</c>).</summary>
    public async Task<DiscountCandidate?> QuoteCandidateAsync(Guid rideCategoryId, Guid? pickupZoneId, BookingType bookingType, decimal baseFare, CancellationToken ct)
    {
        var rule = await ResolveRuleAsync(rideCategoryId, pickupZoneId, bookingType, baseFare, ct);
        var amount = rule?.AmountFor(baseFare) ?? 0m;
        return rule is null || amount <= 0 ? null : new DiscountCandidate(DiscountSources.FavoriteDriver, null, amount, rule.StackableWithPromotions);
    }

    /// <summary>
    /// The favourite driver accepted the trip: <c>favorite_status = accepted</c>, the matching rule is pinned (<c>favorite_discount_rule_id</c>) and <c>estimated_fare</c>
    /// drops to the discounted total (promo code included). No rule, or "offer your price", leaves the fare untouched. Returns the pinned rule.
    /// </summary>
    public async Task<FavoriteDriverDiscountRule?> AcceptAsync(Trip trip, CancellationToken ct)
    {
        trip.FavoriteStatus = FavoriteStatus.Accepted;
        if (trip.PricingMode == PricingMode.Offer)
        {
            return null;
        }

        var quote = await db.FareQuotes.AsNoTracking().FirstOrDefaultAsync(q => q.UsedTripId == trip.Id, ct);
        PromoTripContext? context = quote is null ? null : await promotions.ContextOfQuoteAsync(quote, trip.PaymentMethod, trip.BookingType, trip.PricingMode, ct);
        var zoneId = context?.ZoneId ?? (await zones.ResolveAsync(trip.PickupLat, trip.PickupLng, clock.UtcNow, ct))?.Id;
        var rule = await ResolveRuleAsync(trip.RideCategoryId, zoneId, trip.BookingType, context?.BaseFare, ct);
        if (rule is null)
        {
            return null;
        }

        trip.FavoriteDiscountRuleId = rule.Id;
        if (context?.BaseFare is { } baseFare)
        {
            var favorite = new DiscountCandidate(DiscountSources.FavoriteDriver, null, rule.AmountFor(baseFare), rule.StackableWithPromotions);
            var promo = await promotions.ReservedCandidateAsync(trip.Id, baseFare, context.BookingFee, ct);
            trip.EstimatedFare = engine.Combine(baseFare, promo, favorite).Total;
        }

        return rule;
    }

    /// <summary>
    /// The discount of a completing trip: only when the favourite driver is the assigned one (<c>favorite_status = accepted</c>) with the rule pinned at acceptance (still
    /// honoured after it was deactivated); <c>min_fare</c> is re-checked on the final base fare; none with "offer your price".
    /// </summary>
    public async Task<FavoriteCompletion?> ForCompletionAsync(Trip trip, FareCalculation calculation, CancellationToken ct)
    {
        if (trip.FavoriteStatus != FavoriteStatus.Accepted || trip.FavoriteDiscountRuleId is not { } ruleId || trip.PricingMode == PricingMode.Offer
            || trip.DriverId is null || trip.DriverId != trip.FavoriteDriverId)
        {
            return null;
        }

        var rule = await db.FavoriteDriverDiscountRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ruleId, ct);
        if (rule is null || (rule.MinFare is { } minFare && calculation.Base < minFare))
        {
            return null;
        }

        var amount = rule.AmountFor(calculation.Base);
        return amount > 0 ? new FavoriteCompletion(rule, amount) : null;
    }
}
