using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Domain.Promotions;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Promotions;

/// <summary>What is known about the trip a code is checked against (<c>null</c> = unknown, the check is skipped).</summary>
public sealed record PromoTripContext(
    Guid? CityId, Guid? RideCategoryId, Guid? ZoneId, PaymentMethodKind? PaymentMethod, BookingType? BookingType, PricingMode? PricingMode, decimal? BaseFare,
    decimal BookingFee, bool Corporate = false);

/// <summary>The passenger's history relevant to one promotion.</summary>
public sealed record PromoPassengerState(DateTime UserCreatedAt, bool HasCompletedTrips, int UserRedemptions, bool OtherFirstTripReservation, decimal OutstandingReserved);

/// <summary>Result of the ordered validation of doc 10 §F15.5 (the first failure wins).</summary>
public sealed record PromoCheck(Promotion? Promotion, string? ErrorCode, object? Details, decimal? Amount)
{
    public bool IsValid => ErrorCode is null;

    /// <summary>The <c>reason</c> shown by the quote: the eligibility reason, else the error code.</summary>
    public string? Reason => ErrorCode == ErrorCodes.PromoNotEligible && Details is PromoReason r ? r.Reason : ErrorCode;

    public void ThrowIfInvalid()
    {
        if (ErrorCode is not null)
        {
            throw new DomainException(ErrorCode, Details);
        }
    }
}

public sealed record PromoReason(string Reason);

public sealed record PromoScope(string Scope);

/// <summary>A validated code about to be reserved with a new trip.</summary>
public sealed record PromotionReservationPlan(Promotion Promotion, decimal? Amount, decimal? TotalAfter);

/// <summary>The reservation of a completing trip and the discount recomputed on the final fare.</summary>
public sealed record PromotionCompletion(PromotionRedemption Redemption, Promotion Promotion, bool Eligible, decimal Amount)
{
    public DiscountCandidate? Candidate => Eligible && Amount > 0 ? new DiscountCandidate(DiscountSources.Promotion, Promotion.Code, Amount, Promotion.IsStackable) : null;
}

/// <summary>
/// Promo codes (doc 10 §F15.5): validation in a fixed order, reservation with the trip request (atomic <c>usage_count</c> increment), application on the
/// final fare at completion, release on cancellation / no drivers / payment failure.
/// </summary>
public sealed class PromotionService(AtaDbContext db, IClock clock, ICurrentUser currentUser, IDiscountEngine discounts)
{
    public static class Reasons
    {
        public const string FirstTripOnly = "first_trip_only";
        public const string NewUsersOnly = "new_users_only";
        public const string City = "city";
        public const string Category = "category";
        public const string Zone = "zone";
        public const string PaymentMethod = "payment_method";
        public const string BookingType = "booking_type";
        public const string MinFare = "min_fare";
        public const string PricingMode = "pricing_mode";
    }

    public async Task<Promotion?> FindAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = Promotion.Normalize(code);
        return await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == normalized, ct);
    }

    public async Task<PromoPassengerState> StateAsync(Promotion promotion, Guid passengerId, CancellationToken ct)
    {
        var userCreatedAt = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.Id == passengerId select u.CreatedAt).FirstAsync(ct);
        var completed = await db.Trips.AsNoTracking().AnyAsync(t => t.PassengerId == passengerId && t.Status == TripStatus.Completed, ct);
        var mine = await db.PromotionRedemptions.AsNoTracking()
            .CountAsync(r => r.PassengerId == passengerId && r.PromotionId == promotion.Id && r.Status != RedemptionStatus.Released, ct);
        var otherFirstTrip = promotion.FirstTripOnly && await (from r in db.PromotionRedemptions.AsNoTracking()
                                                               join p in db.Promotions.AsNoTracking() on r.PromotionId equals p.Id
                                                               where r.PassengerId == passengerId && r.Status == RedemptionStatus.Reserved && p.FirstTripOnly
                                                               select r.Id).AnyAsync(ct);
        var outstanding = promotion.BudgetAmount is null
            ? 0m
            : await db.PromotionRedemptions.AsNoTracking().Where(r => r.PromotionId == promotion.Id && r.Status == RedemptionStatus.Reserved)
                .SumAsync(r => r.ReservedAmount ?? 0m, ct);
        return new PromoPassengerState(userCreatedAt, completed, mine, otherFirstTrip, outstanding);
    }

    /// <summary>
    /// Ordered checks: exists and active (<c>404 promo_not_found</c>) → validity window (<c>promo_expired</c>) → total usage and budget
    /// (<c>promo_usage_limit_reached</c>) → per-user limit (<c>{ scope: "user" }</c>) → eligibility (<c>promo_not_eligible { reason }</c>).
    /// </summary>
    public static PromoCheck Check(Promotion? p, PromoPassengerState? state, PromoTripContext ctx, DateTime now)
    {
        if (p is null || !p.IsActive || state is null)
        {
            return new PromoCheck(p, ErrorCodes.PromoNotFound, null, null);
        }

        if (!p.IsWithinValidity(now))
        {
            return new PromoCheck(p, ErrorCodes.PromoExpired, new { p.ValidFrom, p.ValidTo }, null);
        }

        decimal? amount = ctx.BaseFare is { } baseFare ? p.AmountFor(baseFare, ctx.BookingFee) : null;
        if (p.TotalUsageLimit is { } limit && p.UsageCount >= limit)
        {
            return new PromoCheck(p, ErrorCodes.PromoUsageLimitReached, new PromoScope("total"), amount);
        }

        if (p.BudgetAmount is { } budget && (amount is { } a ? p.SpentAmount + state.OutstandingReserved + a > budget : p.SpentAmount + state.OutstandingReserved >= budget))
        {
            return new PromoCheck(p, ErrorCodes.PromoUsageLimitReached, new PromoScope("budget"), amount);
        }

        if (state.UserRedemptions >= p.PerUserLimit)
        {
            return new PromoCheck(p, ErrorCodes.PromoUsageLimitReached, new PromoScope("user"), amount);
        }

        var reason = EligibilityFailure(p, state, ctx, now);
        return reason is null ? new PromoCheck(p, null, null, amount) : new PromoCheck(p, ErrorCodes.PromoNotEligible, new PromoReason(reason), amount);
    }

    private static string? EligibilityFailure(Promotion p, PromoPassengerState state, PromoTripContext ctx, DateTime now)
    {
        if (p.FirstTripOnly && (state.HasCompletedTrips || state.OtherFirstTripReservation)) return Reasons.FirstTripOnly;
        if (p.NewUsersOnly && state.UserCreatedAt < now.AddDays(-p.NewUserDays)) return Reasons.NewUsersOnly;
        if (p.CityId is { } city && ctx.CityId is { } tripCity && city != tripCity) return Reasons.City;
        if (JsonLists.Parse<Guid>(p.RideCategoryIds) is { Count: > 0 } categories && ctx.RideCategoryId is { } category && !categories.Contains(category)) return Reasons.Category;
        if (JsonLists.Parse<Guid>(p.ZoneIds) is { Count: > 0 } zones && ctx.ZoneId is { } zone && !zones.Contains(zone)) return Reasons.Zone;
        if (ctx.Corporate) return Reasons.PaymentMethod;
        if (JsonLists.Parse<PaymentMethodKind>(p.PaymentMethods) is { Count: > 0 } methods && ctx.PaymentMethod is { } method && !methods.Contains(method)) return Reasons.PaymentMethod;
        if (JsonLists.Parse<BookingType>(p.BookingTypes) is { Count: > 0 } bookings && ctx.BookingType is { } booking && !bookings.Contains(booking)) return Reasons.BookingType;
        if (p.MinFare is { } minFare && ctx.BaseFare is { } baseFare && baseFare < minFare) return Reasons.MinFare;
        if (ctx.PricingMode == Domain.Trips.PricingMode.Offer) return Reasons.PricingMode;
        return null;
    }

    public async Task<PromoCheck> EvaluateAsync(string code, Guid passengerId, PromoTripContext ctx, CancellationToken ct)
    {
        var promotion = await FindAsync(code, ct);
        var state = promotion is { IsActive: true } ? await StateAsync(promotion, passengerId, ct) : null;
        return Check(promotion, state, ctx, clock.UtcNow);
    }

    /// <summary>Full validation before a trip is created with <c>promoCode</c> (throws the first failure; no trip is created).</summary>
    public async Task<PromotionReservationPlan> PrepareReservationAsync(string code, Guid passengerId, PromoTripContext ctx, CancellationToken ct)
    {
        var check = await EvaluateAsync(code, passengerId, ctx, ct);
        check.ThrowIfInvalid();
        if (check.Amount is { } a && ctx.BaseFare is { } baseFare)
        {
            var outcome = discounts.Combine(baseFare, new DiscountCandidate(DiscountSources.Promotion, check.Promotion!.Code, a, check.Promotion.IsStackable));
            return new PromotionReservationPlan(check.Promotion, outcome.Discount, outcome.Total);
        }

        return new PromotionReservationPlan(check.Promotion!, check.Amount, null);
    }

    /// <summary>
    /// Reserves the code for the trip inside the trip-creation transaction: <c>UPDATE promotions SET usage_count = usage_count + 1 WHERE id = ? AND
    /// (total_usage_limit IS NULL OR usage_count &lt; total_usage_limit)</c> (no row = used up) + a <c>reserved</c> redemption.
    /// </summary>
    public async Task ReserveAsync(PromotionReservationPlan plan, Guid tripId, Guid passengerId, CancellationToken ct)
    {
        var id = plan.Promotion.Id;
        var reserved = await db.Promotions.Where(p => p.Id == id && (p.TotalUsageLimit == null || p.UsageCount < p.TotalUsageLimit))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.UsageCount, p => p.UsageCount + 1), ct);
        if (reserved == 0)
        {
            throw new DomainException(ErrorCodes.PromoUsageLimitReached, new PromoScope("total"));
        }

        db.PromotionRedemptions.Add(new PromotionRedemption
        {
            PromotionId = id, PassengerId = passengerId, TripId = tripId, Status = RedemptionStatus.Reserved, ReservedAmount = plan.Amount, ReservedAt = clock.UtcNow,
        });
    }

    /// <summary>The reserved code of a completing trip with the discount recomputed on <paramref name="calculation"/> (min fare re-checked).</summary>
    public async Task<PromotionCompletion?> ForCompletionAsync(Trip trip, FareCalculation calculation, CancellationToken ct)
    {
        var redemption = await db.PromotionRedemptions.FirstOrDefaultAsync(r => r.TripId == trip.Id && r.Status == RedemptionStatus.Reserved, ct);
        if (redemption is null)
        {
            return null;
        }

        var promotion = await db.Promotions.AsNoTracking().FirstAsync(p => p.Id == redemption.PromotionId, ct);
        var eligible = trip.PricingMode != PricingMode.Offer && (promotion.MinFare is not { } minFare || calculation.Base >= minFare);
        return new PromotionCompletion(redemption, promotion, eligible, eligible ? promotion.AmountFor(calculation.Base, calculation.Breakdown.BookingFee) : 0m);
    }

    /// <summary>The discount of the trip's still-reserved code on <paramref name="baseFare"/> (F16 uses it to price a trip once the favourite driver accepted); <c>null</c> without a reservation.</summary>
    public async Task<DiscountCandidate?> ReservedCandidateAsync(Guid tripId, decimal baseFare, decimal bookingFee, CancellationToken ct)
    {
        var promotionId = await db.PromotionRedemptions.AsNoTracking().Where(r => r.TripId == tripId && r.Status == RedemptionStatus.Reserved)
            .Select(r => (Guid?)r.PromotionId).FirstOrDefaultAsync(ct);
        if (promotionId is not { } id)
        {
            return null;
        }

        var promotion = await db.Promotions.AsNoTracking().FirstAsync(p => p.Id == id, ct);
        var amount = promotion.AmountFor(baseFare, bookingFee);
        return amount > 0 ? new DiscountCandidate(DiscountSources.Promotion, promotion.Code, amount, promotion.IsStackable) : null;
    }

    /// <summary>Inside the completion transaction: <c>applied</c> + <c>spent_amount += discount</c>, or released (not eligible / not stacked).</summary>
    public async Task CommitCompletionAsync(PromotionCompletion completion, DiscountOutcome? outcome, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var redemption = completion.Redemption;
        if (!completion.Eligible || outcome?.PromotionDropped == true)
        {
            await ReleaseAsync(db, redemption.TripId, completion.Eligible ? RedemptionReleaseReason.NotStacked : RedemptionReleaseReason.NotEligibleAtCompletion, now, ct);
            await db.Entry(redemption).ReloadAsync(ct);
            return;
        }

        var amount = outcome?.AmountOf(DiscountSources.Promotion) ?? 0m;
        redemption.Status = RedemptionStatus.Applied;
        redemption.DiscountAmount = amount;
        redemption.AppliedAt = now;
        var id = completion.Promotion.Id;
        await db.Promotions.Where(p => p.Id == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.SpentAmount, p => p.SpentAmount + amount), ct);
    }

    /// <summary>
    /// Releases the trip's reservation (idempotent): <c>released</c> with <paramref name="reason"/> and <c>usage_count − 1</c>. Runs immediately
    /// (inside the caller's transaction when there is one).
    /// </summary>
    public static async Task ReleaseAsync(AtaDbContext db, Guid tripId, RedemptionReleaseReason reason, DateTime now, CancellationToken ct)
    {
        var promotionId = await db.PromotionRedemptions.AsNoTracking().Where(r => r.TripId == tripId && r.Status == RedemptionStatus.Reserved)
            .Select(r => (Guid?)r.PromotionId).FirstOrDefaultAsync(ct);
        if (promotionId is not { } id)
        {
            return;
        }

        var released = await db.PromotionRedemptions.Where(r => r.TripId == tripId && r.Status == RedemptionStatus.Reserved)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, RedemptionStatus.Released)
                .SetProperty(r => r.ReleasedAt, now)
                .SetProperty(r => r.ReleaseReason, reason), ct);
        if (released > 0)
        {
            await db.Promotions.Where(p => p.Id == id && p.UsageCount > 0).ExecuteUpdateAsync(s => s.SetProperty(p => p.UsageCount, p => p.UsageCount - 1), ct);
        }
    }

    /// <summary>Maps a system cancellation reason code to the release reason.</summary>
    public static RedemptionReleaseReason ReleaseReasonFor(string reasonCode) => reasonCode switch
    {
        "no_drivers" => RedemptionReleaseReason.NoDrivers,
        "payment_failed" => RedemptionReleaseReason.PaymentFailed,
        _ => RedemptionReleaseReason.TripCancelled,
    };

    /// <summary><c>Trip.promotion</c>: the code used by the trip with its reservation status.</summary>
    public async Task<TripPromotionDto?> ForTripAsync(Guid tripId, CancellationToken ct) =>
        await (from r in db.PromotionRedemptions.AsNoTracking()
               join p in db.Promotions.AsNoTracking() on r.PromotionId equals p.Id
               where r.TripId == tripId
               select new TripPromotionDto(p.Code, r.Status, r.DiscountAmount, p.Id, r.ReservedAmount)).FirstOrDefaultAsync(ct);

    /// <summary>The trip context of a stored quote (base, booking fee, category, pickup zone and its city).</summary>
    public async Task<PromoTripContext> ContextOfQuoteAsync(FareQuote quote, PaymentMethodKind? paymentMethod, BookingType? bookingType, PricingMode? pricingMode, CancellationToken ct)
    {
        var bookingFee = 0m;
        try
        {
            bookingFee = JsonSerializer.Deserialize<FareBreakdownDto>(quote.Breakdown, JsonDefaults.Options)?.BookingFee ?? 0m;
        }
        catch (JsonException)
        {
            // Older rows: the booking fee is only needed by free_booking_fee codes.
        }

        var cityId = quote.PickupZoneId is { } zoneId ? await db.Zones.AsNoTracking().Where(z => z.Id == zoneId).Select(z => (Guid?)z.CityId).FirstOrDefaultAsync(ct) : null;
        return new PromoTripContext(cityId, quote.RideCategoryId, quote.PickupZoneId, paymentMethod, bookingType, pricingMode,
            quote.BaseAmount > 0 ? quote.BaseAmount : quote.Total, bookingFee);
    }

    // ----- passenger endpoints -----

    /// <summary>
    /// <c>GET /passenger/promotions</c>: public, active, currently valid codes the passenger may still use (<c>status=available</c>, the default).
    /// Beyond doc 10: <c>status=used</c> (codes the passenger reserved or applied) and <c>status=expired</c> (public codes expired in the last 30 days).
    /// </summary>
    public async Task<IReadOnlyList<PassengerPromotionDto>> ListForPassengerAsync(string? status, Language lang, CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        var now = clock.UtcNow;
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        PassengerPromotionDto ToDto(Promotion p, string rowStatus)
        {
            var codes = JsonLists.Parse<Guid>(p.RideCategoryIds)?.Select(id => categories.GetValueOrDefault(id)).OfType<string>().ToList();
            return new PassengerPromotionDto(p.Code, lang.Pick(p.NameAr, p.NameEn), lang.PickOptional(p.DescriptionAr, p.DescriptionEn), p.Type, p.Value, p.MaxDiscount,
                p.MinFare, p.ValidTo, p.FirstTripOnly, codes is { Count: > 0 } ? codes : null, JsonLists.Parse<PaymentMethodKind>(p.PaymentMethods), rowStatus);
        }

        switch (status ?? "available")
        {
            case "used":
            {
                var used = db.PromotionRedemptions.AsNoTracking().Where(r => r.PassengerId == passengerId && r.Status != RedemptionStatus.Released).Select(r => r.PromotionId);
                var rows = await db.Promotions.AsNoTracking().Where(p => used.Contains(p.Id)).OrderByDescending(p => p.ValidTo).ToListAsync(ct);
                return rows.Select(p => ToDto(p, "used")).ToList();
            }

            case "expired":
            {
                var since = now.AddDays(-30);
                var rows = await db.Promotions.AsNoTracking().Where(p => p.IsPublic && p.ValidTo < now && p.ValidTo >= since).OrderByDescending(p => p.ValidTo).ToListAsync(ct);
                return rows.Select(p => ToDto(p, "expired")).ToList();
            }

            case "available":
            {
                var rows = await db.Promotions.AsNoTracking()
                    .Where(p => p.IsActive && p.IsPublic && p.ValidFrom <= now && p.ValidTo >= now && (p.TotalUsageLimit == null || p.UsageCount < p.TotalUsageLimit))
                    .OrderBy(p => p.ValidTo).ToListAsync(ct);
                var result = new List<PassengerPromotionDto>(rows.Count);
                foreach (var p in rows)
                {
                    var state = await StateAsync(p, passengerId, ct);
                    if (state.UserRedemptions >= p.PerUserLimit || EligibilityFailure(p, state, new PromoTripContext(null, null, null, null, null, null, null, 0m), now) is not null)
                    {
                        continue;
                    }

                    if (p.BudgetAmount is { } budget && p.SpentAmount + state.OutstandingReserved >= budget)
                    {
                        continue;
                    }

                    result.Add(ToDto(p, "available"));
                }

                return result;
            }

            default:
                throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be available|used|expired" });
        }
    }

    /// <summary><c>POST /passenger/promotions/validate</c>: with <c>quoteId</c> the discount is computed on the quoted base.</summary>
    public async Task<ValidatePromoResponse> ValidateAsync(ValidatePromoRequest request, Language lang, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Code), request.Code, Promotion.CodeMaxLength + 10).ThrowIfInvalid();
        var passengerId = await PassengerIdAsync(ct);
        FareQuote? quote = null;
        if (request.QuoteId is { } quoteId)
        {
            var referenced = await db.FareQuotes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quoteId && q.PassengerId == passengerId, ct);
            new Validator().Rule("quoteId", referenced is not null, "unknown quote").ThrowIfInvalid();
            quote = request.RideCategoryId is { } categoryId && categoryId != referenced!.RideCategoryId
                ? await db.FareQuotes.AsNoTracking().FirstOrDefaultAsync(q => q.GroupId == referenced.GroupId && q.RideCategoryId == categoryId, ct)
                : referenced;
            new Validator().Rule("quoteId", quote is not null, "the quote has no price for this ride category").ThrowIfInvalid();
        }

        var ctx = quote is null
            ? new PromoTripContext(null, request.RideCategoryId, null, request.PaymentMethod, request.BookingType, null, null, 0m)
            : await ContextOfQuoteAsync(quote, request.PaymentMethod, request.BookingType, null, ct);
        var check = await EvaluateAsync(request.Code!, passengerId, ctx, ct);
        check.ThrowIfInvalid();
        var p = check.Promotion!;
        var summary = new PromoSummaryDto(p.Code, lang.Pick(p.NameAr, p.NameEn), p.Type, p.Value, p.MaxDiscount, p.IsStackable);
        if (quote is null || check.Amount is not { } amount || ctx.BaseFare is not { } baseFare)
        {
            return new ValidatePromoResponse(true, summary, null, null, null);
        }

        var outcome = discounts.Combine(baseFare, new DiscountCandidate(DiscountSources.Promotion, p.Code, amount, p.IsStackable));
        return new ValidatePromoResponse(true, summary, outcome.Discount, quote.Total, outcome.Total);
    }

    private async Task<Guid> PassengerIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
