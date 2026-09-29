using ATA.Api.Common;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Common;
using ATA.Domain.Favorites;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Favorites;

/// <summary>The matcher's favourite hook (<c>favorite</c> score weight): the passenger's rows of <c>favorite_drivers</c>.</summary>
public sealed class DbFavoriteDriverProvider(AtaDbContext db) : IFavoriteDriverProvider
{
    public async Task<IReadOnlySet<Guid>> FavoriteDriverIdsAsync(Guid passengerId, CancellationToken ct) =>
        (await db.FavoriteDrivers.AsNoTracking().Where(f => f.PassengerId == passengerId).Select(f => f.DriverId).ToListAsync(ct)).ToHashSet();
}

/// <summary>
/// Passenger favourite drivers (doc 10 §F16.2–3): add after a completed trip with the driver (by driver or trip id), remove, list with the shared-trip history,
/// the driver photo and the currently available favourites with their ETA and discount. Drivers see how many passengers saved them.
/// </summary>
public sealed class FavoriteService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IOptions<FavoritesOptions> options,
    FavoriteMatchingService matching,
    FavoriteDiscountService discounts,
    IFileStorage storage)
{
    private readonly FavoritesOptions _options = options.Value;

    public static string PhotoUrl(Guid driverId) => $"/api/v1/passenger/favorite-drivers/{driverId}/photo";

    public async Task<IReadOnlyList<FavoriteDriverDto>> ListAsync(CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        var rows = await db.FavoriteDrivers.AsNoTracking().Where(f => f.PassengerId == passengerId).OrderByDescending(f => f.CreatedAt).ThenBy(f => f.Id).ToListAsync(ct);
        return await ToDtosAsync(passengerId, rows, ct);
    }

    /// <summary>
    /// <c>POST /passenger/favorite-drivers</c>: by <c>tripId</c> (a completed trip of the passenger, else <c>404</c> / <c>422 favorite_not_eligible</c>) or by <c>driverId</c>
    /// (needs at least one completed trip together, else <c>422 favorite_not_eligible</c>); <c>409 favorite_exists</c>; <c>422 favorites_limit</c>.
    /// </summary>
    public async Task<FavoriteDriverDto> AddAsync(AddFavoriteRequest request, CancellationToken ct)
    {
        var validator = new Validator();
        if ((request.DriverId is null) == (request.TripId is null))
        {
            validator.Fail(request.DriverId is null ? nameof(request.DriverId) : nameof(request.TripId), "provide exactly one of driverId or tripId");
        }

        validator.ThrowIfInvalid();
        var passengerId = await PassengerIdAsync(ct);
        Guid driverId;
        Guid sourceTripId;
        if (request.TripId is { } tripId)
        {
            var trip = await db.Trips.AsNoTracking().Where(t => t.Id == tripId && t.PassengerId == passengerId)
                .Select(t => new { t.Id, t.Status, t.DriverId }).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.NotFound);
            if (trip.Status != TripStatus.Completed || trip.DriverId is not { } tripDriver)
            {
                throw new DomainException(ErrorCodes.FavoriteNotEligible);
            }

            driverId = tripDriver;
            sourceTripId = trip.Id;
        }
        else
        {
            driverId = request.DriverId!.Value;
            var latest = await db.Trips.AsNoTracking().Where(t => t.PassengerId == passengerId && t.DriverId == driverId && t.Status == TripStatus.Completed)
                .OrderByDescending(t => t.CompletedAt).Select(t => (Guid?)t.Id).FirstOrDefaultAsync(ct);
            sourceTripId = latest ?? throw new DomainException(ErrorCodes.FavoriteNotEligible);
        }

        if (await db.FavoriteDrivers.AnyAsync(f => f.PassengerId == passengerId && f.DriverId == driverId, ct))
        {
            throw new DomainException(ErrorCodes.FavoriteExists);
        }

        if (await db.FavoriteDrivers.CountAsync(f => f.PassengerId == passengerId, ct) >= _options.MaxPerPassenger)
        {
            throw new DomainException(ErrorCodes.FavoritesLimit, new { max = _options.MaxPerPassenger });
        }

        var favorite = new FavoriteDriver { PassengerId = passengerId, DriverId = driverId, SourceTripId = sourceTripId };
        db.FavoriteDrivers.Add(favorite);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Added concurrently by another request of the same passenger.
            throw new DomainException(ErrorCodes.FavoriteExists);
        }

        return (await ToDtosAsync(passengerId, [favorite], ct)).Single();
    }

    /// <summary>Removing a favourite never touches a trip that is already running (the trip keeps its <c>favorite_driver_id</c> and pinned rule).</summary>
    public async Task RemoveAsync(Guid driverId, CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        var favorite = Guard.NotFound(await db.FavoriteDrivers.FirstOrDefaultAsync(f => f.PassengerId == passengerId && f.DriverId == driverId, ct));
        db.FavoriteDrivers.Remove(favorite);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsFavoriteAsync(Guid passengerId, Guid driverId, CancellationToken ct) =>
        await db.FavoriteDrivers.AsNoTracking().AnyAsync(f => f.PassengerId == passengerId && f.DriverId == driverId, ct);

    /// <summary>The driver's photo, only for a passenger who saved the driver (<c>404</c> otherwise or without a photo).</summary>
    public async Task<(Stream Content, string ContentType)> PhotoAsync(Guid driverId, CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        if (!await IsFavoriteAsync(passengerId, driverId, ct))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var fileId = await (from doc in db.DriverDocuments.AsNoTracking()
                            join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
                            where doc.DriverId == driverId && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected
                            select (Guid?)doc.FileId).FirstOrDefaultAsync(ct);
        var file = fileId is { } id ? await db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct) : null;
        var stream = file is null ? null : await storage.OpenReadAsync(file.StorageKey, ct);
        return stream is null ? throw new DomainException(ErrorCodes.NotFound) : (stream, file!.ContentType);
    }

    /// <summary>
    /// <c>GET /passenger/favorite-drivers/available</c>: the passenger's favourites that are eligible right now around the pickup for the category
    /// (F9 eligibility) within <c>Favorites:AvailabilityRadiusMeters</c>, nearest first, with the discount rule that would apply. No location is returned.
    /// </summary>
    public async Task<IReadOnlyList<AvailableFavoriteDto>> AvailableAsync(decimal? lat, decimal? lng, Guid? rideCategoryId, CancellationToken ct)
    {
        var validator = new Validator().Require(nameof(lat), lat).Require(nameof(lng), lng).Require(nameof(rideCategoryId), rideCategoryId)
            .Rule(nameof(lat), lat is null or (>= -90 and <= 90), "out of range").Rule(nameof(lng), lng is null or (>= -180 and <= 180), "out of range");
        validator.ThrowIfInvalid();
        var categoryId = rideCategoryId!.Value;
        validator.Rule(nameof(rideCategoryId), await db.RideCategories.AsNoTracking().AnyAsync(c => c.Id == categoryId && c.IsActive, ct), "unknown or inactive ride category").ThrowIfInvalid();

        var passenger = await db.Passengers.AsNoTracking().Where(p => p.UserId == currentUser.UserId).Select(p => new { p.Id, p.PreferFemaleDriver }).FirstOrDefaultAsync(ct)
                        ?? throw new DomainException(ErrorCodes.Forbidden);
        var favoriteIds = await db.FavoriteDrivers.AsNoTracking().Where(f => f.PassengerId == passenger.Id).Select(f => f.DriverId).ToListAsync(ct);
        if (favoriteIds.Count == 0)
        {
            return [];
        }

        var context = await matching.ContextAsync(lat!.Value, lng!.Value, categoryId, ct);
        var candidates = await matching.FindAsync(favoriteIds, lat.Value, lng.Value, categoryId, passenger.PreferFemaleDriver, context.RadiusMeters, null, ct);
        if (candidates.Count == 0)
        {
            return [];
        }

        var rule = await discounts.ResolveRuleAsync(categoryId, context.ZoneId, BookingType.Now, null, ct);
        var discount = rule is null ? null : new FavoriteDiscountDto(rule.DiscountPercent, rule.MaxDiscountAmount, rule.StackableWithPromotions);
        var ids = candidates.Select(c => c.DriverId).ToList();
        var profiles = await ProfilesAsync(ids, ct);
        return candidates.Select(c =>
        {
            var profile = profiles[c.DriverId];
            return new AvailableFavoriteDto(c.DriverId, profile.FirstName, profile.HasPhoto ? PhotoUrl(c.DriverId) : null, profile.RatingAvg, profile.Vehicle,
                Math.Max(1, (int)Math.Ceiling(c.EtaSeconds / 60d)), discount);
        }).ToList();
    }

    /// <summary><c>GET /driver/favorites/count</c>: passengers who saved the driver.</summary>
    public async Task<DriverFavoritesCountDto> DriverCountAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var driverId = await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        return new DriverFavoritesCountDto(await db.FavoriteDrivers.CountAsync(f => f.DriverId == driverId, ct));
    }

    private sealed record DriverCard(string? FirstName, decimal RatingAvg, bool HasPhoto, FavoriteVehicleDto? Vehicle, string? CategoryCode);

    private async Task<Dictionary<Guid, DriverCard>> ProfilesAsync(IReadOnlyCollection<Guid> driverIds, CancellationToken ct)
    {
        var ids = driverIds.ToList();
        var drivers = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                             where ids.Contains(d.Id) select new { d.Id, u.FullName, d.RatingAvg }).ToListAsync(ct);
        var vehicles = await (from v in db.Vehicles.AsNoTracking() join c in db.RideCategories.AsNoTracking() on v.RideCategoryId equals c.Id
                              where v.IsActive && ids.Contains(v.DriverId) select new { v.DriverId, v.Make, v.Model, v.Color, c.Code }).ToListAsync(ct);
        var photos = (await (from doc in db.DriverDocuments.AsNoTracking() join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
                             where ids.Contains(doc.DriverId) && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected
                             select doc.DriverId).ToListAsync(ct)).ToHashSet();
        return drivers.ToDictionary(d => d.Id, d =>
        {
            var vehicle = vehicles.FirstOrDefault(v => v.DriverId == d.Id);
            return new DriverCard(Ratings.RatingService.FirstName(d.FullName), d.RatingAvg, photos.Contains(d.Id),
                vehicle is null ? null : new FavoriteVehicleDto(vehicle.Make, vehicle.Model, vehicle.Color), vehicle?.Code);
        });
    }

    private async Task<IReadOnlyList<FavoriteDriverDto>> ToDtosAsync(Guid passengerId, IReadOnlyList<FavoriteDriver> rows, CancellationToken ct)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var ids = rows.Select(r => r.DriverId).ToList();
        var profiles = await ProfilesAsync(ids, ct);
        var trips = await db.Trips.AsNoTracking().Where(t => t.PassengerId == passengerId && t.Status == TripStatus.Completed && t.DriverId != null && ids.Contains(t.DriverId!.Value))
            .GroupBy(t => t.DriverId!.Value).Select(g => new { DriverId = g.Key, Count = g.Count(), Last = g.Max(t => t.CompletedAt) }).ToDictionaryAsync(x => x.DriverId, ct);
        return rows.Where(r => profiles.ContainsKey(r.DriverId)).Select(r =>
        {
            var card = profiles[r.DriverId];
            var together = trips.GetValueOrDefault(r.DriverId);
            return new FavoriteDriverDto(r.DriverId, card.FirstName, card.HasPhoto ? PhotoUrl(r.DriverId) : null, card.RatingAvg, card.Vehicle, card.CategoryCode,
                together?.Count ?? 0, together?.Last, r.CreatedAt);
        }).ToList();
    }

    private async Task<Guid> PassengerIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
