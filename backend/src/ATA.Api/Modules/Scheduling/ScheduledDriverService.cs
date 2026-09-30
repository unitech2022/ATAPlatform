using ATA.Api.Common;
using ATA.Api.Modules.Airports;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Scheduling;

/// <summary>
/// Driver side of scheduled rides (doc 11 §F17.3–4): the marketplace (city, radius, category / upgrade, gender, restrictions, cash debt, favourite window; approximate pickup and
/// no passenger identity until reserved), reserve, list, confirm and release.
/// </summary>
public sealed class ScheduledDriverService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    ScheduleRuleProvider rules,
    ScheduledRideEngine engine,
    ZoneResolver zones,
    AirportCatalog airports,
    MatchingSettingsProvider matchingSettings,
    Cancellation.ReliabilityService reliability,
    TripReadService reads,
    IOptions<PayoutsOptions> payouts)
{
    private const int MaxRadiusPadKm = 1;

    // ----- marketplace -----

    public async Task<PagedResult<MarketplaceTripDto>> MarketplaceAsync(decimal? lat, decimal? lng, DateTime? from, DateTime? to, Paging paging, Language lang, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(lat), lat is null or (>= -90 and <= 90), "out of range").Rule(nameof(lng), lng is null or (>= -180 and <= 180), "out of range")
            .Rule("lat", (lat is null) == (lng is null), "lat and lng go together")
            .Rule(nameof(to), from is null || to is null || to >= from, "must not be before 'from'").ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        if (driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        var now = clock.UtcNow;
        (decimal Lat, decimal Lng)? origin = lat is { } la && lng is { } ln ? (la, ln) : null;
        if (origin is null && await db.DriverLocations.AsNoTracking().Where(l => l.DriverId == driver.Id).Select(l => new { l.Lat, l.Lng }).FirstOrDefaultAsync(ct) is { } last)
        {
            origin = (last.Lat, last.Lng);
        }

        if (origin is null)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["lat"] = "required", ["lng"] = "required" });
        }

        var context = await DriverContextAsync(driver, ct);
        if (!context.CanTakeTrips)
        {
            return paging.Result<MarketplaceTripDto>([], 0);
        }

        var ruleRows = await db.ScheduledRideRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var maxRadiusKm = Math.Max(ScheduledRideRule.Default().MarketplaceRadiusKm, ruleRows.Count == 0 ? 0 : ruleRows.Max(r => r.MarketplaceRadiusKm)) + MaxRadiusPadKm;
        var (deltaLat, deltaLng) = Geo.BoundingBox(origin.Value.Lat, maxRadiusKm * 1000d);
        var fromAt = from is { } f && f.ToUniversalTime() > now ? f.ToUniversalTime() : now;
        var query = db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Scheduled && t.BookingType == BookingType.Scheduled && t.ReservedDriverId == null
            && t.ScheduledAt != null && t.ScheduledAt >= fromAt
            && t.PickupLat >= origin.Value.Lat - deltaLat && t.PickupLat <= origin.Value.Lat + deltaLat && t.PickupLng >= origin.Value.Lng - deltaLng && t.PickupLng <= origin.Value.Lng + deltaLng);
        if (to is { } toAt)
        {
            var end = toAt.ToUniversalTime();
            query = query.Where(t => t.ScheduledAt <= end);
        }

        var candidates = await query.OrderBy(t => t.ScheduledAt).ToListAsync(ct);
        var visible = new List<(Trip Trip, ScheduledRideRule Rule, decimal DistanceKm)>();
        foreach (var trip in candidates)
        {
            var rule = await RuleOfAsync(trip, ct);
            var distanceKm = (decimal)Geo.HaversineMeters(origin.Value.Lat, origin.Value.Lng, trip.PickupLat, trip.PickupLng) / 1000m;
            if (distanceKm <= rule.MarketplaceRadiusKm && await IsVisibleAsync(context, trip, rule, now, ct))
            {
                visible.Add((trip, rule, distanceKm));
            }
        }

        var page = visible.Skip(paging.Skip).Take(paging.PageSize).ToList();
        var quotes = await QuotesAsync(page.Select(p => p.Trip.Id).ToList(), ct);
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, ct);
        var allZones = await zones.AllAsync(ct);
        var allAirports = await airports.AllAsync(ct);
        var items = page.Select(p =>
        {
            var (trip, rule, distanceKm) = p;
            var category = categories[trip.RideCategoryId];
            var airport = trip.AirportId is { } airportId ? allAirports.FirstOrDefault(a => a.Id == airportId) : null;
            var pickupZone = zones.Resolve(allZones, trip.PickupLat, trip.PickupLng, now);
            var dropoffZone = zones.Resolve(allZones, trip.DropoffLat, trip.DropoffLng, now);
            var pickupArea = airport is not null && trip.AirportDirection == Domain.Airports.AirportDirection.Pickup ? airport.Name(lang) : pickupZone is null ? null : lang.Pick(pickupZone.NameAr, pickupZone.NameEn);
            var dropoffArea = airport is not null && trip.AirportDirection == Domain.Airports.AirportDirection.Dropoff ? airport.Name(lang) : dropoffZone is null ? null : lang.Pick(dropoffZone.NameAr, dropoffZone.NameEn);
            var isFavorite = trip.FavoriteDriverId == driver.Id;
            DateTime? exclusiveUntil = isFavorite && now < ScheduledRideEngine.ExclusiveUntil(trip, rule) ? ScheduledRideEngine.ExclusiveUntil(trip, rule) : null;
            return new MarketplaceTripDto(
                trip.Id, trip.ScheduledAt!.Value, new MarketplaceCategoryDto(category.Code, lang.Pick(category.NameAr, category.NameEn)), pickupArea,
                new ApproxPointDto(decimal.Round(trip.PickupLat, 3), decimal.Round(trip.PickupLng, 3)), dropoffArea, decimal.Round(distanceKm, 1), trip.EstimatedDistanceM,
                trip.EstimatedFare, quotes.GetValueOrDefault(trip.Id), trip.AirportId is not null, isFavorite, exclusiveUntil);
        }).ToList();
        return paging.Result(items, visible.Count);
    }

    // ----- reservations -----

    public async Task<ReservationDto> ReserveAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        if (driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        await reliability.EnsureNotRestrictedAsync(driver.UserId, Role.Driver, ct);
        var balance = await db.Wallets.AsNoTracking().Where(w => w.UserId == driver.UserId && w.Kind == WalletKind.Driver).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(ct) ?? 0m;
        if (-balance > payouts.Value.MaxCashDebt)
        {
            throw new DomainException(ErrorCodes.CashDebtLimitExceeded, new { cashDebt = -balance, limit = payouts.Value.MaxCashDebt });
        }

        var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId && t.BookingType == BookingType.Scheduled, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (trip.ReservedDriverId is not null)
        {
            throw new DomainException(ErrorCodes.ReservationTaken);
        }

        var rule = await RuleOfAsync(trip, ct);
        var context = await DriverContextAsync(driver, ct);
        if (trip.Status == TripStatus.Scheduled && !await IsVisibleAsync(context, trip, rule, clock.UtcNow, ct))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var (reservation, _) = await engine.ReserveAsync(tripId, driver, ReservationSource.Marketplace, rule, enforceLimit: true, ct);
        return (await ToDtosAsync([reservation], lang, ct))[0];
    }

    /// <summary><c>GET /driver/scheduled?status=active|history&amp;page=</c>: <c>active</c> = reserved / confirmed / assigned by <c>scheduledAt</c>, <c>history</c> = the rest, newest first.</summary>
    public async Task<PagedResult<ReservationDto>> ListAsync(string? status, Paging paging, Language lang, CancellationToken ct)
    {
        var filter = status?.Trim().ToLowerInvariant();
        new Validator().Rule(nameof(status), filter is null or "active" or "history", "must be active|history").ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        var query = from r in db.ScheduledRideReservations.AsNoTracking() join t in db.Trips.AsNoTracking() on r.TripId equals t.Id where r.DriverId == driver.Id select new { r, t.ScheduledAt };
        var active = filter != "history";
        query = active
            ? query.Where(x => x.r.Status == ReservationStatus.Reserved || x.r.Status == ReservationStatus.Confirmed || x.r.Status == ReservationStatus.Assigned)
            : query.Where(x => x.r.Status != ReservationStatus.Reserved && x.r.Status != ReservationStatus.Confirmed && x.r.Status != ReservationStatus.Assigned);
        var total = await query.CountAsync(ct);
        var rows = active
            ? await query.OrderBy(x => x.ScheduledAt).ThenBy(x => x.r.ReservedAt).Skip(paging.Skip).Take(paging.PageSize).Select(x => x.r).ToListAsync(ct)
            : await query.OrderByDescending(x => x.r.UpdatedAt).ThenBy(x => x.r.Id).Skip(paging.Skip).Take(paging.PageSize).Select(x => x.r).ToListAsync(ct);
        return paging.Result(await ToDtosAsync(rows, lang, ct), total);
    }

    public async Task<ReservationDto> ConfirmAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var (reservation, assigned) = await engine.ConfirmAsync(tripId, driver, ct);
        var dto = (await ToDtosAsync([reservation], lang, ct))[0];
        return assigned is null ? dto : dto with { Trip = await reads.BuildAsync(assigned, TripViewer.Driver, lang, ct) };
    }

    public async Task<ReservationDto> ReleaseAsync(Guid tripId, ReleaseReservationRequest? request, Language lang, CancellationToken ct)
    {
        new Validator().Rule("reason", request?.Reason is null || request.Reason.Length <= 500, "max_length:500").ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        var reservation = await engine.DriverReleaseAsync(tripId, driver, request?.Reason?.Trim(), ct);
        return (await ToDtosAsync([reservation], lang, ct))[0];
    }

    // ----- visibility -----

    private sealed record DriverContext(DriverProfile Driver, Guid? CategoryId, int CategorySort, bool CanTakeTrips);

    private async Task<DriverContext> DriverContextAsync(DriverProfile driver, CancellationToken ct)
    {
        var vehicle = await (from v in db.Vehicles.AsNoTracking() join c in db.RideCategories.AsNoTracking() on v.RideCategoryId equals c.Id
                             where v.DriverId == driver.Id && v.IsActive select new { v.RideCategoryId, c.SortOrder }).FirstOrDefaultAsync(ct);
        var restricted = reliability.IsRestricted(await reliability.GetAsync(driver.UserId, Role.Driver, ct));
        var balance = await db.Wallets.AsNoTracking().Where(w => w.UserId == driver.UserId && w.Kind == WalletKind.Driver).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(ct) ?? 0m;
        var indebted = -balance > payouts.Value.MaxCashDebt;
        return new DriverContext(driver, vehicle?.RideCategoryId, vehicle?.SortOrder ?? 0, vehicle is not null && !restricted && !indebted);
    }

    /// <summary>
    /// Marketplace visibility of a scheduled trip for a driver: before the search window, the marketplace enabled (or the requested favourite), the favourite's exclusive window, the
    /// driver's city, category (or a higher one when <c>allow_category_upgrade</c>) and the female-driver preference.
    /// </summary>
    private async Task<bool> IsVisibleAsync(DriverContext context, Trip trip, ScheduledRideRule rule, DateTime now, CancellationToken ct)
    {
        var driver = context.Driver;
        if (context is not { CanTakeTrips: true, CategoryId: { } categoryId } || trip.ScheduledAt is not { } scheduledAt || now >= rule.SearchStartsAt(scheduledAt))
        {
            return false;
        }

        var isFavorite = trip.FavoriteDriverId == driver.Id;
        if (!isFavorite && (!rule.MarketplaceEnabled || (trip.FavoriteDriverId is not null && now < ScheduledRideEngine.ExclusiveUntil(trip, rule))))
        {
            return false;
        }

        if (trip.PreferFemaleDriver && driver.Gender != Gender.Female)
        {
            return false;
        }

        var pickupZone = await zones.ResolveAsync(trip.PickupLat, trip.PickupLng, now, ct);
        if (driver.CityId is { } driverCity && pickupZone is not null && pickupZone.CityId != driverCity)
        {
            return false;
        }

        if (categoryId == trip.RideCategoryId)
        {
            return true;
        }

        var tripCategorySort = await db.RideCategories.AsNoTracking().Where(c => c.Id == trip.RideCategoryId).Select(c => (int?)c.SortOrder).FirstOrDefaultAsync(ct);
        return tripCategorySort is { } sort && context.CategorySort >= sort && (await matchingSettings.ResolveAsync(pickupZone?.Id, trip.RideCategoryId, ct)).AllowCategoryUpgrade;
    }

    // ----- mapping -----

    private async Task<IReadOnlyList<ReservationDto>> ToDtosAsync(IReadOnlyList<ScheduledRideReservation> reservations, Language lang, CancellationToken ct)
    {
        var tripIds = reservations.Select(r => r.TripId).Distinct().ToList();
        var trips = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        var passengerIds = trips.Values.Select(t => t.PassengerId).Distinct().ToList();
        var passengerNames = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where passengerIds.Contains(p.Id) select new { p.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var quotes = await QuotesAsync(tripIds, ct);
        var result = new List<ReservationDto>(reservations.Count);
        foreach (var reservation in reservations)
        {
            var trip = trips[reservation.TripId];
            var rule = await RuleOfAsync(trip, ct);
            var exact = reservation.IsActive || reservation.Status == ReservationStatus.Completed;
            ReservationPlaceDto Place(string name, string address, decimal placeLat, decimal placeLng) =>
                exact ? new ReservationPlaceDto(name, address, placeLat, placeLng) : new ReservationPlaceDto(null, null, decimal.Round(placeLat, 3), decimal.Round(placeLng, 3));
            result.Add(new ReservationDto(
                reservation.Id, reservation.TripId, reservation.Status, reservation.Source, trip.ScheduledAt ?? trip.RequestedAt,
                Place(trip.PickupName, trip.PickupAddress, trip.PickupLat, trip.PickupLng), Place(trip.DropoffName, trip.DropoffAddress, trip.DropoffLat, trip.DropoffLng),
                exact ? Ratings.RatingService.FirstName(passengerNames.GetValueOrDefault(trip.PassengerId)) : null, trip.EstimatedFare, quotes.GetValueOrDefault(trip.Id),
                reservation.ConfirmDeadline(rule), reservation.FinalConfirmDeadline(rule), rule.FreeReleaseUntil(trip.ScheduledAt ?? trip.RequestedAt), reservation.ReservedAt,
                reservation.PenaltyPoints, reservation.ReleaseReason));
        }

        return result;
    }

    private async Task<Dictionary<Guid, decimal>> QuotesAsync(IReadOnlyCollection<Guid> tripIds, CancellationToken ct)
    {
        if (tripIds.Count == 0)
        {
            return [];
        }

        var rows = await db.FareQuotes.AsNoTracking().Where(q => q.UsedTripId != null && tripIds.Contains(q.UsedTripId.Value)).Select(q => new { TripId = q.UsedTripId!.Value, q.DriverNetEarnings }).ToListAsync(ct);
        return rows.ToDictionary(r => r.TripId, r => r.DriverNetEarnings);
    }

    private readonly Dictionary<(Guid?, Guid), ScheduledRideRule> _ruleCache = [];

    private async Task<ScheduledRideRule> RuleOfAsync(Trip trip, CancellationToken ct)
    {
        var city = await rules.CityOfAsync(trip.PickupLat, trip.PickupLng, trip.RequestedAt, ct);
        if (!_ruleCache.TryGetValue((city, trip.RideCategoryId), out var rule))
        {
            rule = await rules.ResolveAsync(city, trip.RideCategoryId, ct);
            _ruleCache[(city, trip.RideCategoryId)] = rule;
        }

        return rule;
    }

    private async Task<DriverProfile> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
