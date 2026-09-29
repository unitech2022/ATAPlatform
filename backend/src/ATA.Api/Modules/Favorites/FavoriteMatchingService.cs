using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Favorites;

/// <summary>What the favourite search needs to know about a pickup: its zone, whether the exclusive round is enabled there and the search radius.</summary>
public sealed record FavoriteSearchContext(Guid? ZoneId, bool ExclusiveEnabled, int RadiusMeters);

/// <summary>
/// The matcher side of F16 (doc 10 §F16.2): the eligibility search restricted to favourite drivers, used by <c>/passenger/favorite-drivers/available</c>, by the trip
/// request (immediate <c>unavailable</c>) and by the exclusive round 0 of <c>MatchingService</c>. Eligibility is the F9 one (<see cref="IMatcher"/>): online, approved, free,
/// fresh location, category (or higher when allowed), gender, zone, documents, F14 restrictions and cash debt — inside <c>Favorites:AvailabilityRadiusMeters</c>.
/// </summary>
public sealed class FavoriteMatchingService(IMatcher matcher, MatchingSettingsProvider settings, ZoneResolver zones, IClock clock, IOptions<FavoritesOptions> options)
{
    private readonly FavoritesOptions _options = options.Value;

    /// <summary>Radius of the exclusive round / the availability list for a pickup whose matching settings are <paramref name="resolved"/>.</summary>
    public int RadiusFor(ResolvedMatchingSettings resolved) => _options.AvailabilityRadiusMeters ?? resolved.RadiusMeters;

    public int ExclusiveTimeoutSeconds => Math.Max(1, _options.ExclusiveOfferTimeoutSeconds);

    public async Task<FavoriteSearchContext> ContextAsync(decimal lat, decimal lng, Guid rideCategoryId, CancellationToken ct)
    {
        var zone = await zones.ResolveAsync(lat, lng, clock.UtcNow, ct);
        var resolved = await settings.ResolveAsync(zone?.Id, rideCategoryId, ct);
        return new FavoriteSearchContext(zone?.Id, resolved.PreferFavoriteDriver, RadiusFor(resolved));
    }

    /// <summary>Eligible drivers among <paramref name="driverIds"/> around the pickup, nearest first.</summary>
    public async Task<IReadOnlyList<DriverCandidate>> FindAsync(
        IReadOnlyCollection<Guid> driverIds, decimal lat, decimal lng, Guid rideCategoryId, bool preferFemaleDriver, int radiusMeters, Guid? tripId, CancellationToken ct)
    {
        if (driverIds.Count == 0)
        {
            return [];
        }

        var found = await matcher.FindCandidatesAsync(new MatchCriteria(lat, lng, rideCategoryId, preferFemaleDriver, [], radiusMeters, null, tripId, driverIds), ct);
        return found.OrderBy(c => c.DistanceMeters).ToList();
    }

    /// <summary>The favourite driver as a candidate of the exclusive round, or <c>null</c> when not eligible right now.</summary>
    public async Task<DriverCandidate?> FindExclusiveAsync(Trip trip, Guid favoriteDriverId, int radiusMeters, CancellationToken ct) =>
        (await FindAsync([favoriteDriverId], trip.PickupLat, trip.PickupLng, trip.RideCategoryId, trip.PreferFemaleDriver, radiusMeters, trip.Id, ct)).FirstOrDefault();

    /// <summary>
    /// <c>favorite_status</c> of a new request: <c>unavailable</c> when the zone/category turned <c>prefer_favorite_driver</c> off or (for "now" bookings) the driver is
    /// not eligible at this moment, else <c>requested</c>. Scheduled trips are checked when their search starts.
    /// </summary>
    public async Task<(FavoriteStatus Status, string? Reason)> InitialStatusAsync(
        Guid favoriteDriverId, decimal lat, decimal lng, Guid rideCategoryId, bool preferFemaleDriver, BookingType bookingType, CancellationToken ct)
    {
        var context = await ContextAsync(lat, lng, rideCategoryId, ct);
        if (!context.ExclusiveEnabled)
        {
            return (FavoriteStatus.Unavailable, "prefer_favorite_driver_disabled");
        }

        if (bookingType == BookingType.Now
            && (await FindAsync([favoriteDriverId], lat, lng, rideCategoryId, preferFemaleDriver, context.RadiusMeters, null, ct)).Count == 0)
        {
            return (FavoriteStatus.Unavailable, "not_eligible");
        }

        return (FavoriteStatus.Requested, null);
    }
}
