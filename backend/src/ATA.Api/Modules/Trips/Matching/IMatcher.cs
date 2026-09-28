using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips.Matching;

public sealed record MatchCriteria(decimal PickupLat, decimal PickupLng, Guid RideCategoryId, bool PreferFemaleDriver, IReadOnlyCollection<Guid> ExcludedDriverIds);

public sealed record DriverCandidate(Guid DriverId, Guid UserId, Guid VehicleId, int DistanceMeters, int EtaSeconds);

/// <summary>Finds eligible drivers for a pickup, nearest first. <see cref="SimpleMatcher"/> is replaced by the F9 engine.</summary>
public interface IMatcher
{
    Task<IReadOnlyList<DriverCandidate>> FindCandidatesAsync(MatchCriteria criteria, CancellationToken ct);
}

/// <summary>
/// Online, approved drivers without a current trip whose active vehicle is in the requested category (or a higher one when
/// <c>Matching:AllowUpgrade</c>), female when requested, within <c>Matching:RadiusMeters</c>, ordered by Haversine distance.
/// The SQL query only applies a bounding box; the exact distance is computed in memory so it runs on MySQL and SQLite alike.
/// </summary>
public sealed class SimpleMatcher(AtaDbContext db, IPricingService pricing, IOptions<MatchingOptions> options, IClock clock) : IMatcher
{
    private readonly MatchingOptions _options = options.Value;

    public async Task<IReadOnlyList<DriverCandidate>> FindCandidatesAsync(MatchCriteria criteria, CancellationToken ct)
    {
        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == criteria.RideCategoryId, ct);
        if (category is null)
        {
            return [];
        }

        var categoryIds = _options.AllowUpgrade
            ? await db.RideCategories.AsNoTracking().Where(c => c.IsActive && c.SortOrder >= category.SortOrder).Select(c => c.Id).ToListAsync(ct)
            : [category.Id];

        var (deltaLat, deltaLng) = Geo.BoundingBox(criteria.PickupLat, _options.RadiusMeters);
        var minLat = criteria.PickupLat - deltaLat;
        var maxLat = criteria.PickupLat + deltaLat;
        var minLng = criteria.PickupLng - deltaLng;
        var maxLng = criteria.PickupLng + deltaLng;
        var freshAfter = clock.UtcNow.AddSeconds(-_options.LocationMaxAgeSeconds);
        var excluded = criteria.ExcludedDriverIds.ToList();

        var query = from loc in db.DriverLocations.AsNoTracking()
                    join d in db.Drivers.AsNoTracking() on loc.DriverId equals d.Id
                    join v in db.Vehicles.AsNoTracking() on d.Id equals v.DriverId
                    where loc.IsOnline && d.IsOnline && d.ApplicationStatus == ApplicationStatus.Approved && d.CurrentTripId == null
                          && v.IsActive && categoryIds.Contains(v.RideCategoryId)
                          && loc.UpdatedAt >= freshAfter
                          && loc.Lat >= minLat && loc.Lat <= maxLat && loc.Lng >= minLng && loc.Lng <= maxLng
                          && !excluded.Contains(d.Id)
                    select new { d.Id, d.UserId, VehicleId = v.Id, loc.Lat, loc.Lng, d.Gender };

        if (criteria.PreferFemaleDriver)
        {
            query = query.Where(x => x.Gender == Gender.Female);
        }

        var rows = await query.ToListAsync(ct);
        return rows
            .Select(r => (Row: r, Distance: Geo.HaversineMeters(criteria.PickupLat, criteria.PickupLng, r.Lat, r.Lng)))
            .Where(x => x.Distance <= _options.RadiusMeters)
            .OrderBy(x => x.Distance)
            .Select(x => new DriverCandidate(x.Row.Id, x.Row.UserId, x.Row.VehicleId, (int)Math.Round(x.Distance), pricing.EtaSeconds(x.Distance * FlatPricing.RoadFactor)))
            .ToList();
    }
}
