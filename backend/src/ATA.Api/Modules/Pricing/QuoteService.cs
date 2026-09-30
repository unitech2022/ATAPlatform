using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Pricing;

/// <summary>
/// Prices a route for every active category. <c>POST /pricing/quote</c> stores one <c>fare_quotes</c> row per category (one group,
/// 5-minute expiry) so the trip request can lock the price; <c>/admin/pricing/simulate</c> runs the same calculation at any time without storing.
/// </summary>
public sealed class QuoteService(
    AtaDbContext db, IPricingService pricing, IMatcher matcher, IClock clock, IOptions<PricingOptions> options,
    Promotions.PromotionService promotions, Promotions.IDiscountEngine discounts, Favorites.FavoriteDiscountService favoriteDiscounts, Favorites.FavoriteService favorites,
    Scheduling.ScheduleRuleProvider scheduleRules, Airports.AirportTripService airportTrips)
{
    private readonly PricingOptions _options = options.Value;

    /// <summary>What a quote knows about the trip beyond the route: a scheduled booking (its rule may lock the demand level to <c>normal</c>) and whether it touches an airport.</summary>
    private sealed record QuoteContext(bool Scheduled, Guid? CityId, bool AirportTrip);

    /// <param name="corporate">F19: the trip is paid by a company, so neither a promo code nor a favourite-driver discount is priced in (doc 10 §F15.1).</param>
    public async Task<QuoteResponse> QuoteAsync(EstimateRequest request, Guid passengerId, Language lang, CancellationToken ct, bool corporate = false)
    {
        if (corporate)
        {
            request = request with { PromoCode = null, FavoriteDriverId = null };
        }

        var now = clock.UtcNow;
        new Validator().Route(request.Pickup, request.Dropoff, request.Stops, requireLabels: false).Booking(request.BookingType, request.ScheduledAt).ThrowIfInvalid();
        var scheduled = request.BookingType == BookingType.Scheduled;
        // F17: an airport pickup zone replaces the pickup point; the zone is optional for a quote (a trip request requires it).
        var airport = await airportTrips.PrepareAsync(request.Pickup!.Lat!.Value, request.Pickup.Lng!.Value, request.Dropoff!.Lat!.Value, request.Dropoff.Lng!.Value,
            new Airports.AirportRequestInput(request.AirportPickupZoneId, request.AirportTerminalCode, request.FlightNumber), requirePickupZone: false, ct);
        if (airport?.PickupZone is { } zone)
        {
            request = request with { Pickup = new PlaceRequest(request.Pickup.Name, request.Pickup.Address, zone.Lat, zone.Lng) };
        }

        if (request.RideCategoryId is { } requestedCategory && airport is null
            && await db.RideCategories.AsNoTracking().AnyAsync(c => c.Id == requestedCategory && c.Code == Airports.AirportTripService.AirportCategoryCode, ct))
        {
            throw new DomainException(ErrorCodes.AirportCategoryNotApplicable);
        }

        Guid? cityId = null;
        if (scheduled)
        {
            var pickup = request.Pickup!.Point();
            cityId = await scheduleRules.CityOfAsync(pickup.Lat, pickup.Lng, now, ct);
            Scheduling.ScheduleRuleProvider.EnsureWindow(await scheduleRules.ResolveAsync(cityId, request.RideCategoryId, ct), request.ScheduledAt!.Value, now);
        }

        var at = scheduled ? request.ScheduledAt!.Value.ToUniversalTime() : now;
        var (response, quotes) = await BuildAsync(request, at, passengerId, lang, new QuoteContext(scheduled, cityId, airport is not null), ct);
        db.FareQuotes.AddRange(quotes);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<QuoteResponse> SimulateAsync(SimulateRequest request, Language lang, CancellationToken ct)
    {
        var now = clock.UtcNow;
        new Validator().Route(request.Pickup, request.Dropoff, request.Stops, requireLabels: false).ThrowIfInvalid();
        var at = (request.At ?? request.ScheduledAt ?? now).ToUniversalTime();
        var estimate = new EstimateRequest(request.Pickup, request.Dropoff, request.Stops, request.RideCategoryId, request.BookingType, request.ScheduledAt);
        var scheduled = request.BookingType == BookingType.Scheduled;
        var cityId = scheduled ? await scheduleRules.CityOfAsync(request.Pickup!.Lat!.Value, request.Pickup.Lng!.Value, now, ct) : null;
        var (response, _) = await BuildAsync(estimate, at, null, lang, new QuoteContext(scheduled, cityId, AirportTrip: true), ct);
        return response;
    }

    /// <summary>Prices one category for a trip request made without a quote and returns the (already used) quote row to persist with the trip.</summary>
    public async Task<FareQuote> QuoteForTripAsync(
        RideCategory category, GeoPoint pickup, GeoPoint dropoff, RouteEstimate route, DateTime at, Guid passengerId, CancellationToken ct, DemandReading? lockedDemand = null)
    {
        var calculation = await pricing.CalculateAsync(new FareRequest(category, pickup, dropoff, route.DistanceMeters, route.DurationSeconds, at, LockedDemand: lockedDemand), ct);
        return ToRecord(calculation, Guid.CreateVersion7(), passengerId, category.Id, route, at);
    }

    public FareQuote ToRecord(FareCalculation calculation, Guid groupId, Guid passengerId, Guid rideCategoryId, RouteEstimate route, DateTime at) => new()
    {
        GroupId = groupId,
        PassengerId = passengerId,
        RideCategoryId = rideCategoryId,
        PickupZoneId = calculation.PickupZone?.Id,
        DropoffZoneId = calculation.DropoffZone?.Id,
        DistanceM = route.DistanceMeters,
        DurationS = route.DurationSeconds,
        Breakdown = JsonSerializer.Serialize(ToDto(calculation.Breakdown), JsonDefaults.Options),
        DemandLevelCode = calculation.Demand.Code,
        Total = calculation.Total,
        BaseAmount = PricingMath.Round2(calculation.Base),
        DriverNetEarnings = calculation.DriverNetEarnings,
        DriverSharePercent = calculation.DriverSharePercent,
        OfferMin = calculation.OfferMin,
        OfferMax = calculation.OfferMax,
        PricingRuleId = calculation.PricingRuleId,
        ExpiresAt = clock.UtcNow.AddMinutes(_options.QuoteExpiryMinutes),
    };

    /// <summary>Rebuilds the demand reading a stored quote was priced with, so completion keeps the accepted multiplier.</summary>
    public static DemandReading? LockedDemandOf(FareQuote quote)
    {
        try
        {
            var breakdown = JsonSerializer.Deserialize<FareBreakdownDto>(quote.Breakdown, JsonDefaults.Options);
            return breakdown is null ? null : DemandReading.Neutral() with { Code = quote.DemandLevelCode, Multiplier = breakdown.DemandMultiplier, LevelMultiplier = breakdown.DemandMultiplier };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static FareBreakdownDto ToDto(FareBreakdown b) =>
        new(b.BaseFare, b.DistanceFare, b.TimeFare, b.WaitingFare, b.MinFareApplied, b.TimeMultiplier, b.TimeMultiplierLabel, b.DemandMultiplier, b.BookingFee, b.ServiceFee, b.Discount);

    public static DemandDto ToDto(DemandReading d, Language lang) => new(d.Code, lang.Pick(d.NameAr, d.NameEn), d.Multiplier, d.Color, d.Source);

    public static ZoneRefDto? ToDto(ZoneSnapshot? z, Language lang) => z is null ? null : new ZoneRefDto(z.Id, z.Code, lang.Pick(z.NameAr, z.NameEn));

    private async Task<(QuoteResponse Response, List<FareQuote> Quotes)> BuildAsync(EstimateRequest request, DateTime at, Guid? passengerId, Language lang, QuoteContext context, CancellationToken ct)
    {
        // F17: the airport category is only offered for trips that touch an airport.
        var categories = (await db.RideCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct))
            .Where(c => context.AirportTrip || c.Code != Airports.AirportTripService.AirportCategoryCode).ToList();
        if (request.RideCategoryId is { } requestedId)
        {
            new Validator().Rule(nameof(request.RideCategoryId), categories.Any(c => c.Id == requestedId), "unknown or inactive ride category").ThrowIfInvalid();
        }

        var pickup = request.Pickup!.Point();
        var dropoff = request.Dropoff!.Point();
        var route = pricing.EstimateRoute(pickup, (request.Stops ?? []).Select(s => s.Point()).ToList(), dropoff);
        var groupId = Guid.CreateVersion7();
        var now = clock.UtcNow;
        // F15: a promo code is checked per category (an invalid code never fails the quote); stored quotes keep the undiscounted price.
        var promoCode = passengerId is null || string.IsNullOrWhiteSpace(request.PromoCode) ? null : Domain.Promotions.Promotion.Normalize(request.PromoCode);
        var promotion = promoCode is null ? null : await promotions.FindAsync(promoCode, ct);
        var promoState = promotion is { IsActive: true } && passengerId is { } promoPassenger ? await promotions.StateAsync(promotion, promoPassenger, ct) : null;
        var promoChecks = new Dictionary<Guid, Promotions.PromoCheck>();
        // F16: the favourite-driver discount is shown assuming the favourite accepts (`favoriteDiscountConditional`); the driver must be one of the passenger's favourites.
        if (request.FavoriteDriverId is { } favoriteId && passengerId is { } favoritePassenger)
        {
            new Validator().Rule(nameof(request.FavoriteDriverId), await favorites.IsFavoriteAsync(favoritePassenger, favoriteId, ct), "not_favorite").ThrowIfInvalid();
        }
        else
        {
            request = request with { FavoriteDriverId = null };
        }

        var favoriteByCategory = new HashSet<Guid>();
        var promotionDropped = new HashSet<Guid>();
        var quotes = new List<FareQuote>(categories.Count);
        var items = new List<QuoteCategoryDto>(categories.Count);
        FareCalculation? first = null;
        foreach (var category in categories)
        {
            // F17: a scheduled booking is priced with the time multipliers at `scheduled_at` and a locked `normal` demand level (no surge) when its rule says so.
            var lockedDemand = context.Scheduled && (await scheduleRules.ResolveAsync(context.CityId, category.Id, ct)).LockDemandNormal ? DemandReading.Neutral() : null;
            var calculation = await pricing.CalculateAsync(new FareRequest(category, pickup, dropoff, route.DistanceMeters, route.DurationSeconds, at, LockedDemand: lockedDemand), ct);
            first ??= calculation;
            var candidates = await matcher.FindCandidatesAsync(new MatchCriteria(pickup.Lat, pickup.Lng, category.Id, false, []), ct);
            int? eta = candidates.Count == 0 ? null : Math.Max(1, (int)Math.Ceiling(candidates.Min(c => c.EtaSeconds) / 60d));
            Guid? quoteId = null;
            if (passengerId is { } owner)
            {
                var quote = ToRecord(calculation, groupId, owner, category.Id, route, at);
                quotes.Add(quote);
                quoteId = quote.Id;
            }

            var total = calculation.Total;
            var breakdown = ToDto(calculation.Breakdown);
            decimal? totalBefore = null;
            Promotions.DiscountCandidate? promoCandidate = null;
            if (promoCode is not null)
            {
                var ctx = new Promotions.PromoTripContext(calculation.PickupZone?.CityId, category.Id, calculation.PickupZone?.Id, null, request.BookingType ?? BookingType.Now, null,
                    calculation.Base, calculation.Breakdown.BookingFee);
                var check = Promotions.PromotionService.Check(promotion, promoState, ctx, now);
                promoChecks[category.Id] = check;
                if (check is { IsValid: true, Amount: > 0 })
                {
                    promoCandidate = new Promotions.DiscountCandidate(Promotions.DiscountSources.Promotion, promotion!.Code, check.Amount.Value, promotion.IsStackable);
                }
            }

            var favoriteCandidate = request.FavoriteDriverId is null
                ? null
                : await favoriteDiscounts.QuoteCandidateAsync(category.Id, calculation.PickupZone?.Id, request.BookingType ?? BookingType.Now, calculation.Base, ct);
            if (promoCandidate is not null || favoriteCandidate is not null)
            {
                var outcome = discounts.Combine(calculation.Base, promoCandidate, favoriteCandidate);
                totalBefore = total;
                total = outcome.Total;
                breakdown = breakdown with { Discount = outcome.Discount, Discounts = Promotions.DiscountEngine.Lines(outcome, lang) };
                if (outcome.AmountOf(Promotions.DiscountSources.FavoriteDriver) > 0)
                {
                    favoriteByCategory.Add(category.Id);
                }

                if (outcome.PromotionDropped)
                {
                    promotionDropped.Add(category.Id);
                }
            }

            items.Add(new QuoteCategoryDto(category.Id, category.Code, lang.Pick(category.NameAr, category.NameEn), eta, total, calculation.DriverNetEarnings,
                calculation.OfferMin, calculation.OfferMax, breakdown, ToDto(calculation.Demand, lang), quoteId, calculation.Source, totalBefore));
        }

        var primaryIndex = request.RideCategoryId is { } id ? Math.Max(0, items.FindIndex(i => i.RideCategoryId == id)) : 0;
        var primary = items.Count > 0 ? items[primaryIndex] : null;
        var response = new QuoteResponse(
            primary?.QuoteId,
            quotes.Count > 0 ? quotes[0].ExpiresAt : null,
            route.DistanceMeters,
            route.DurationSeconds,
            ToDto(first?.PickupZone, lang),
            ToDto(first?.DropoffZone, lang),
            primary?.Demand ?? ToDto(DemandReading.Neutral(), lang),
            items,
            promoCode is null ? null : PromotionOf(promoCode, primary is null ? null : promoChecks.GetValueOrDefault(primary.RideCategoryId), primary is not null && promotionDropped.Contains(primary.RideCategoryId)),
            primary is not null && favoriteByCategory.Contains(primary.RideCategoryId));
        return (response, quotes);
    }

    /// <summary><paramref name="notStacked"/>: the code is valid but the larger, non-combinable favourite-driver discount wins (<c>reason = not_stacked</c>, F16).</summary>
    private static Promotions.QuotePromotionDto PromotionOf(string code, Promotions.PromoCheck? check, bool notStacked) =>
        check is null ? new(code, false, ErrorCodes.PromoNotFound) : new(check.Promotion?.Code ?? code, check.IsValid, check.IsValid ? (notStacked ? "not_stacked" : null) : check.Reason);
}
