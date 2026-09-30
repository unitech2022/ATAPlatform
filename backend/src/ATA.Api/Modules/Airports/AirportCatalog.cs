using ATA.Api.Common;
using ATA.Domain.Airports;
using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Airports;

/// <summary>Parsed, active <c>airport_zones</c> row; <see cref="Polygon"/> is set for driver waiting areas.</summary>
public sealed record AirportZoneSnapshot(
    Guid Id, Guid AirportId, AirportZoneKind Kind, string Code, string? TerminalCode, string NameAr, string NameEn, GeoPolygon? Polygon, decimal Lat, decimal Lng,
    string? InstructionsAr, string? InstructionsEn, int? FreeWaitingMinutes, decimal? WaitingPerMinute, int SortOrder);

/// <summary>Parsed, active airport with its active zones.</summary>
public sealed record AirportSnapshot(
    Guid Id, Guid CityId, string Code, string NameAr, string NameEn, decimal Lat, decimal Lng, GeoPolygon Geofence, bool RequiresPickupZone, int? DefaultFreeWaitingMinutes,
    decimal? DefaultWaitingPerMinute, bool QueueEnabled, IReadOnlyList<AirportZoneSnapshot> Zones)
{
    public IEnumerable<AirportZoneSnapshot> Terminals => Zones.Where(z => z.Kind == AirportZoneKind.Terminal);

    public IEnumerable<AirportZoneSnapshot> PickupZones => Zones.Where(z => z.Kind == AirportZoneKind.PickupZone);

    public IEnumerable<AirportZoneSnapshot> WaitingAreas => Zones.Where(z => z.Kind == AirportZoneKind.DriverWaitingArea && z.Polygon is not null);

    public string Name(Language lang) => lang.Pick(NameAr, NameEn);

    public bool ContainsPoint(decimal lat, decimal lng) => Geofence.Contains(lat, lng);
}

/// <summary>Process-wide cache of the parsed airports (geofences are tested on every driver location update); invalidated by the admin endpoints.</summary>
public sealed class AirportCache
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<AirportSnapshot>? _airports;

    public void Invalidate() => Volatile.Write(ref _airports, null);

    public async Task<IReadOnlyList<AirportSnapshot>> GetOrLoadAsync(Func<CancellationToken, Task<IReadOnlyList<AirportSnapshot>>> loader, CancellationToken ct)
    {
        var cached = Volatile.Read(ref _airports);
        if (cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(ct);
        try
        {
            cached = Volatile.Read(ref _airports);
            if (cached is null)
            {
                cached = await loader(ct);
                Volatile.Write(ref _airports, cached);
            }

            return cached;
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>Point-in-geofence lookup over the active airports (doc 11 §F17.7 "الكشف").</summary>
public sealed class AirportCatalog(AtaDbContext db, AirportCache cache)
{
    public Task<IReadOnlyList<AirportSnapshot>> AllAsync(CancellationToken ct) => cache.GetOrLoadAsync(LoadAsync, ct);

    public async Task<AirportSnapshot?> FindAsync(Guid airportId, CancellationToken ct) => (await AllAsync(ct)).FirstOrDefault(a => a.Id == airportId);

    /// <summary>The active airport whose geofence contains the point (the nearest centre when geofences overlap).</summary>
    public async Task<AirportSnapshot?> AtAsync(decimal lat, decimal lng, CancellationToken ct) =>
        (await AllAsync(ct)).Where(a => a.ContainsPoint(lat, lng)).OrderBy(a => Geo.HaversineMeters(lat, lng, a.Lat, a.Lng)).FirstOrDefault();

    /// <summary>The queue-enabled airport whose driver waiting area contains the point, with that area.</summary>
    public async Task<(AirportSnapshot Airport, AirportZoneSnapshot Area)?> WaitingAreaAtAsync(decimal lat, decimal lng, CancellationToken ct)
    {
        foreach (var airport in await AllAsync(ct))
        {
            if (!airport.QueueEnabled)
            {
                continue;
            }

            var area = airport.WaitingAreas.FirstOrDefault(a => a.Polygon!.Contains(lat, lng));
            if (area is not null)
            {
                return (airport, area);
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<AirportSnapshot>> LoadAsync(CancellationToken ct)
    {
        var airports = await db.Airports.AsNoTracking().Where(a => a.IsActive).ToListAsync(ct);
        var zones = await db.AirportZones.AsNoTracking().Where(z => z.IsActive).ToListAsync(ct);
        var snapshots = new List<AirportSnapshot>(airports.Count);
        foreach (var airport in airports)
        {
            if (!GeoPolygon.TryParse(airport.Geofence, out var ring) || ring is null)
            {
                continue;
            }

            var own = zones.Where(z => z.AirportId == airport.Id).OrderBy(z => z.SortOrder).ThenBy(z => z.Code)
                .Select(z =>
                {
                    GeoPolygon? polygon = null;
                    GeoPolygon.TryParse(z.Polygon, out polygon);
                    return new AirportZoneSnapshot(z.Id, z.AirportId, z.Kind, z.Code, z.TerminalCode, z.NameAr, z.NameEn, polygon, z.Lat, z.Lng, z.InstructionsAr, z.InstructionsEn,
                        z.FreeWaitingMinutes, z.WaitingPerMinute, z.SortOrder);
                }).ToList();
            snapshots.Add(new AirportSnapshot(airport.Id, airport.CityId, airport.Code, airport.NameAr, airport.NameEn, airport.Lat, airport.Lng, ring, airport.RequiresPickupZone,
                airport.DefaultFreeWaitingMinutes, airport.DefaultWaitingPerMinute, airport.QueueEnabled, own));
        }

        return snapshots;
    }
}
