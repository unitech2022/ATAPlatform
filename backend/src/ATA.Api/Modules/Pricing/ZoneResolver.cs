using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Pricing;

/// <summary>Process-wide cache of parsed zones; invalidated by the admin zone endpoints.</summary>
public sealed class ZoneCache
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IReadOnlyList<ZoneSnapshot>? _zones;

    public void Invalidate() => Volatile.Write(ref _zones, null);

    public async Task<IReadOnlyList<ZoneSnapshot>> GetOrLoadAsync(Func<CancellationToken, Task<IReadOnlyList<ZoneSnapshot>>> loader, CancellationToken ct)
    {
        var cached = Volatile.Read(ref _zones);
        if (cached is not null)
        {
            return cached;
        }

        await _gate.WaitAsync(ct);
        try
        {
            cached = Volatile.Read(ref _zones);
            if (cached is null)
            {
                cached = await loader(ct);
                Volatile.Write(ref _zones, cached);
            }

            return cached;
        }
        finally
        {
            _gate.Release();
        }
    }
}

/// <summary>
/// Point-in-polygon (ray casting) zone lookup: the active, open zone with the highest priority wins on overlap; a point outside every
/// zone falls back to the nearest city's <c>city_default</c> zone.
/// </summary>
public sealed class ZoneResolver(AtaDbContext db, ZoneCache cache, IOptions<PricingOptions> options)
{
    private readonly PricingOptions _options = options.Value;

    public Task<IReadOnlyList<ZoneSnapshot>> AllAsync(CancellationToken ct) => cache.GetOrLoadAsync(LoadAsync, ct);

    public async Task<ZoneSnapshot?> ResolveAsync(decimal lat, decimal lng, DateTime atUtc, CancellationToken ct) =>
        Resolve(await AllAsync(ct), lat, lng, atUtc);

    public async Task<ZoneSnapshot?> FindAsync(Guid zoneId, CancellationToken ct) => (await AllAsync(ct)).FirstOrDefault(z => z.Id == zoneId);

    /// <summary>Pure lookup over an already loaded zone list (used in loops so the list is read once).</summary>
    public ZoneSnapshot? Resolve(IReadOnlyList<ZoneSnapshot> zones, decimal lat, decimal lng, DateTime atUtc)
    {
        var local = PricingMath.ToLocal(atUtc, _options);
        var containing = zones
            .Where(z => z.Ring.Contains(lat, lng) && z.Schedule.IsOpenAt(local))
            .OrderByDescending(z => z.Priority)
            .ThenBy(z => z.IsCityDefault ? 1 : 0)
            .FirstOrDefault();
        if (containing is not null)
        {
            return containing;
        }

        return zones
            .Where(z => z.IsCityDefault)
            .OrderBy(z => Geo.HaversineMeters(lat, lng, z.CenterLat, z.CenterLng))
            .FirstOrDefault();
    }

    private async Task<IReadOnlyList<ZoneSnapshot>> LoadAsync(CancellationToken ct)
    {
        var zones = await db.Zones.AsNoTracking().Include(z => z.CategorySettings).Where(z => z.IsActive).ToListAsync(ct);
        var snapshots = new List<ZoneSnapshot>(zones.Count);
        foreach (var zone in zones)
        {
            if (!GeoPolygon.TryParse(zone.Polygon, out var ring) || ring is null)
            {
                continue;
            }

            snapshots.Add(new ZoneSnapshot(
                zone.Id, zone.CityId, zone.Code, zone.NameAr, zone.NameEn, zone.Priority, zone.IsCityDefault, zone.CenterLat, zone.CenterLng,
                ring, OperatingSchedule.Parse(zone.OperatingHours),
                zone.CategorySettings.ToDictionary(s => s.RideCategoryId, s => new ZoneCategoryRule(s.IsEnabled, s.SurgeCap))));
        }

        return snapshots;
    }
}
