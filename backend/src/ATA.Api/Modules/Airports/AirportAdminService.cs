using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Airports;
using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Airports;

/// <summary>Public airport catalogue and the passenger "resolve" lookup (doc 11 §F17.8).</summary>
public sealed class AirportReadService(AirportCatalog catalog)
{
    public async Task<IReadOnlyList<AirportCatalogDto>> CatalogAsync(Language lang, CancellationToken ct) =>
        (await catalog.AllAsync(ct)).OrderBy(a => a.Code).Select(a => new AirportCatalogDto(a.Id, a.Code, a.Name(lang), a.Lat, a.Lng, Terminals(a, lang), PickupZones(a, lang))).ToList();

    public async Task<AirportResolveDto?> ResolveAsync(decimal? lat, decimal? lng, Language lang, CancellationToken ct)
    {
        new Validator().Require(nameof(lat), lat).Require(nameof(lng), lng)
            .Rule(nameof(lat), lat is null or (>= -90 and <= 90), "out of range").Rule(nameof(lng), lng is null or (>= -180 and <= 180), "out of range").ThrowIfInvalid();
        var airport = await catalog.AtAsync(lat!.Value, lng!.Value, ct);
        return airport is null
            ? null
            : new AirportResolveDto(new AirportRefDto(airport.Id, airport.Code, airport.Name(lang)), airport.RequiresPickupZone, PickupZones(airport, lang), Terminals(airport, lang));
    }

    private static IReadOnlyList<AirportTerminalDto> Terminals(AirportSnapshot a, Language lang) =>
        a.Terminals.Select(t => new AirportTerminalDto(t.Id, t.Code, t.TerminalCode, lang.Pick(t.NameAr, t.NameEn))).ToList();

    private static IReadOnlyList<AirportPickupZoneDto> PickupZones(AirportSnapshot a, Language lang) =>
        a.PickupZones.Select(z => new AirportPickupZoneDto(z.Id, z.Code, z.TerminalCode, lang.Pick(z.NameAr, z.NameEn), z.Lat, z.Lng,
            lang.PickOptional(z.InstructionsAr, z.InstructionsEn), z.FreeWaitingMinutes ?? a.DefaultFreeWaitingMinutes)).ToList();
}

/// <summary>
/// Admin console of airports, their zones and the live queue (<c>airport.manage</c>). Every write is audited (<c>airport.*</c>, <c>airport_zone.*</c>, <c>airport_queue.remove</c>)
/// and drops the airport cache; an airport / zone that trips reference is deactivated instead of deleted.
/// </summary>
public sealed partial class AirportAdminService(AtaDbContext db, AuditService audit, AirportCache cache, AirportQueueService queue, AirportQueueBroadcastState broadcasts, IClock clock)
{
    private const string AirportEntity = "airport";
    private const string ZoneEntity = "airport_zone";
    private const string QueueEntity = "airport_queue";

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Iata();

    [GeneratedRegex("^[A-Za-z0-9_-]{1,30}$")]
    private static partial Regex ZoneCode();

    // ----- airports -----

    public async Task<IReadOnlyList<AdminAirportDto>> ListAsync(CancellationToken ct)
    {
        var airports = await db.Airports.AsNoTracking().OrderBy(a => a.Code).ToListAsync(ct);
        return await ToDtosAsync(airports, ct);
    }

    public async Task<AdminAirportDto> GetAsync(Guid id, CancellationToken ct)
    {
        var airport = Guard.NotFound(await db.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct));
        return (await ToDtosAsync([airport], ct))[0];
    }

    public async Task<AdminAirportDto> CreateAsync(AirportUpsertRequest request, CancellationToken ct)
    {
        var code = await ValidateAirportAsync(request, null, ct);
        var airport = new Airport { Code = code, NameAr = string.Empty, NameEn = string.Empty, Geofence = string.Empty };
        ApplyAirport(airport, request, code);
        db.Airports.Add(airport);
        audit.Log("airport.create", AirportEntity, airport.Id, null, Snapshot(airport));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return (await ToDtosAsync([airport], ct))[0];
    }

    public async Task<AdminAirportDto> UpdateAsync(Guid id, AirportUpsertRequest request, CancellationToken ct)
    {
        var airport = Guard.NotFound(await db.Airports.FirstOrDefaultAsync(a => a.Id == id, ct));
        var code = await ValidateAirportAsync(request, airport, ct);
        var before = Snapshot(airport);
        ApplyAirport(airport, request, code);
        audit.Log("airport.update", AirportEntity, airport.Id, before, Snapshot(airport));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return (await ToDtosAsync([airport], ct))[0];
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var airport = Guard.NotFound(await db.Airports.FirstOrDefaultAsync(a => a.Id == id, ct));
        var before = Snapshot(airport);
        if (await db.Trips.AnyAsync(t => t.AirportId == id, ct))
        {
            airport.IsActive = false;
            audit.Log("airport.delete", AirportEntity, airport.Id, before, new { deactivated = true, isActive = false });
        }
        else
        {
            db.Airports.Remove(airport);
            audit.Log("airport.delete", AirportEntity, airport.Id, before, null);
        }

        await db.SaveChangesAsync(ct);
        cache.Invalidate();
    }

    // ----- zones -----

    public async Task<IReadOnlyList<AdminAirportZoneDto>> ZonesAsync(Guid airportId, CancellationToken ct)
    {
        await EnsureAirportAsync(airportId, ct);
        var zones = await db.AirportZones.AsNoTracking().Where(z => z.AirportId == airportId).OrderBy(z => z.SortOrder).ThenBy(z => z.Code).ToListAsync(ct);
        return zones.Select(ToDto).ToList();
    }

    public async Task<AdminAirportZoneDto> CreateZoneAsync(Guid airportId, AirportZoneUpsertRequest request, CancellationToken ct)
    {
        await EnsureAirportAsync(airportId, ct);
        var code = await ValidateZoneAsync(airportId, request, null, ct);
        var zone = new AirportZone { AirportId = airportId, Code = code, NameAr = string.Empty, NameEn = string.Empty };
        ApplyZone(zone, request, code);
        db.AirportZones.Add(zone);
        audit.Log("airport_zone.create", ZoneEntity, zone.Id, null, ZoneSnapshot(zone));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return ToDto(zone);
    }

    public async Task<AdminAirportZoneDto> UpdateZoneAsync(Guid airportId, Guid zoneId, AirportZoneUpsertRequest request, CancellationToken ct)
    {
        var zone = Guard.NotFound(await db.AirportZones.FirstOrDefaultAsync(z => z.Id == zoneId && z.AirportId == airportId, ct));
        var code = await ValidateZoneAsync(airportId, request, zone, ct);
        var before = ZoneSnapshot(zone);
        ApplyZone(zone, request, code);
        audit.Log("airport_zone.update", ZoneEntity, zone.Id, before, ZoneSnapshot(zone));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return ToDto(zone);
    }

    public async Task DeleteZoneAsync(Guid airportId, Guid zoneId, CancellationToken ct)
    {
        var zone = Guard.NotFound(await db.AirportZones.FirstOrDefaultAsync(z => z.Id == zoneId && z.AirportId == airportId, ct));
        var before = ZoneSnapshot(zone);
        if (await db.Trips.AnyAsync(t => t.AirportZoneId == zoneId, ct))
        {
            zone.IsActive = false;
            audit.Log("airport_zone.delete", ZoneEntity, zone.Id, before, new { deactivated = true, isActive = false });
        }
        else
        {
            db.AirportZones.Remove(zone);
            audit.Log("airport_zone.delete", ZoneEntity, zone.Id, before, null);
        }

        await db.SaveChangesAsync(ct);
        cache.Invalidate();
    }

    // ----- queue -----

    public async Task<IReadOnlyList<AdminAirportQueueEntryDto>> QueueAsync(Guid airportId, CancellationToken ct)
    {
        await EnsureAirportAsync(airportId, ct);
        return await queue.AdminListAsync(airportId, ct);
    }

    public async Task RemoveFromQueueAsync(Guid airportId, Guid entryId, RemoveQueueEntryRequest? request, CancellationToken ct)
    {
        new Validator().Rule("reason", request?.Reason is null || request.Reason.Length <= 500, "max_length:500").ThrowIfInvalid();
        var entry = Guard.NotFound(await db.AirportQueueEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.AirportId == airportId, ct));
        if (!entry.IsActive)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = entry.Status });
        }

        var before = new { entry.Status, entry.DriverId, entry.EnteredAt };
        entry.Status = AirportQueueStatus.Removed;
        entry.LeftAt = clock.UtcNow;
        entry.LeftReason = AirportQueueLeftReason.AdminRemoved;
        entry.OfferedTripId = null;
        broadcasts.Forget(entry.Id);
        audit.Log("airport_queue.remove", QueueEntity, entry.Id, before, new { status = entry.Status, reason = request?.Reason?.Trim() });
        await db.SaveChangesAsync(ct);
    }

    // ----- validation / mapping -----

    private async Task EnsureAirportAsync(Guid airportId, CancellationToken ct)
    {
        if (!await db.Airports.AnyAsync(a => a.Id == airportId, ct))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }
    }

    private async Task<string> ValidateAirportAsync(AirportUpsertRequest r, Airport? existing, CancellationToken ct)
    {
        var code = r.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var v = new Validator()
            .Require(nameof(r.CityId), r.CityId)
            .Rule(nameof(r.Code), Iata().IsMatch(code), "must be a 3-letter IATA code")
            .Require(nameof(r.NameAr), r.NameAr, 120).Require(nameof(r.NameEn), r.NameEn, 120)
            .Require(nameof(r.Lat), r.Lat).Require(nameof(r.Lng), r.Lng)
            .Rule(nameof(r.Lat), r.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(r.Lng), r.Lng is null or (>= -180 and <= 180), "out of range")
            .Rule(nameof(r.DefaultFreeWaitingMinutes), r.DefaultFreeWaitingMinutes is null or (>= 0 and <= 240), "must be between 0 and 240")
            .Rule(nameof(r.DefaultWaitingPerMinute), r.DefaultWaitingPerMinute is null or >= 0, "must be positive");
        v.Rule(nameof(r.Geofence), r.Geofence is { ValueKind: JsonValueKind.Array } && GeoPolygon.TryParse(r.Geofence.Value.GetRawText(), out _), "must be a ring [[lat,lng],…] of at least 3 points");
        v.ThrowIfInvalid();
        v.Rule(nameof(r.CityId), await db.Cities.AnyAsync(c => c.Id == r.CityId, ct), "unknown city");
        v.Rule(nameof(r.Code), !await db.Airports.AnyAsync(a => a.Code == code && (existing == null || a.Id != existing.Id), ct), "already exists");
        v.ThrowIfInvalid();
        return code;
    }

    private static void ApplyAirport(Airport airport, AirportUpsertRequest r, string code)
    {
        airport.CityId = r.CityId!.Value;
        airport.Code = code;
        airport.NameAr = r.NameAr!.Trim();
        airport.NameEn = r.NameEn!.Trim();
        airport.Lat = r.Lat!.Value;
        airport.Lng = r.Lng!.Value;
        airport.Geofence = r.Geofence!.Value.GetRawText();
        airport.RequiresPickupZone = r.RequiresPickupZone ?? true;
        airport.DefaultFreeWaitingMinutes = r.DefaultFreeWaitingMinutes;
        airport.DefaultWaitingPerMinute = r.DefaultWaitingPerMinute;
        airport.QueueEnabled = r.QueueEnabled ?? false;
        airport.IsActive = r.IsActive ?? true;
    }

    private async Task<string> ValidateZoneAsync(Guid airportId, AirportZoneUpsertRequest r, AirportZone? existing, CancellationToken ct)
    {
        var code = r.Code?.Trim() ?? string.Empty;
        var terminalCode = string.IsNullOrWhiteSpace(r.TerminalCode) ? null : r.TerminalCode.Trim().ToUpperInvariant();
        var v = new Validator()
            .Require(nameof(r.Kind), r.Kind)
            .Rule(nameof(r.Kind), r.Kind is null || Enum.IsDefined(r.Kind.Value), "must be terminal|pickup_zone|driver_waiting_area")
            .Rule(nameof(r.Code), ZoneCode().IsMatch(code), "1-30 letters, digits, - or _")
            .Rule(nameof(r.TerminalCode), terminalCode is null || terminalCode.Length <= 10, "max_length:10")
            .Rule(nameof(r.TerminalCode), r.Kind != AirportZoneKind.Terminal || terminalCode is not null, "required")
            .Require(nameof(r.NameAr), r.NameAr, 120).Require(nameof(r.NameEn), r.NameEn, 120)
            .Require(nameof(r.Lat), r.Lat).Require(nameof(r.Lng), r.Lng)
            .Rule(nameof(r.Lat), r.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(r.Lng), r.Lng is null or (>= -180 and <= 180), "out of range")
            .Rule(nameof(r.InstructionsAr), r.InstructionsAr is null || r.InstructionsAr.Length <= 500, "max_length:500")
            .Rule(nameof(r.InstructionsEn), r.InstructionsEn is null || r.InstructionsEn.Length <= 500, "max_length:500")
            .Rule(nameof(r.FreeWaitingMinutes), r.FreeWaitingMinutes is null or (>= 0 and <= 240), "must be between 0 and 240")
            .Rule(nameof(r.WaitingPerMinute), r.WaitingPerMinute is null or >= 0, "must be positive");
        var hasPolygon = r.Polygon is { ValueKind: JsonValueKind.Array };
        v.Rule(nameof(r.Polygon), r.Kind != AirportZoneKind.DriverWaitingArea || hasPolygon, "required");
        v.Rule(nameof(r.Polygon), !hasPolygon || GeoPolygon.TryParse(r.Polygon!.Value.GetRawText(), out _), "must be a ring [[lat,lng],…] of at least 3 points");
        v.ThrowIfInvalid();
        v.Rule(nameof(r.Code), !await db.AirportZones.AnyAsync(z => z.AirportId == airportId && z.Code == code && (existing == null || z.Id != existing.Id), ct), "already exists");
        v.ThrowIfInvalid();
        return code;
    }

    private static void ApplyZone(AirportZone zone, AirportZoneUpsertRequest r, string code)
    {
        zone.Kind = r.Kind!.Value;
        zone.Code = code;
        zone.TerminalCode = string.IsNullOrWhiteSpace(r.TerminalCode) ? null : r.TerminalCode.Trim().ToUpperInvariant();
        zone.NameAr = r.NameAr!.Trim();
        zone.NameEn = r.NameEn!.Trim();
        zone.Polygon = r.Polygon is { ValueKind: JsonValueKind.Array } polygon ? polygon.GetRawText() : null;
        zone.Lat = r.Lat!.Value;
        zone.Lng = r.Lng!.Value;
        zone.InstructionsAr = string.IsNullOrWhiteSpace(r.InstructionsAr) ? null : r.InstructionsAr.Trim();
        zone.InstructionsEn = string.IsNullOrWhiteSpace(r.InstructionsEn) ? null : r.InstructionsEn.Trim();
        zone.FreeWaitingMinutes = r.FreeWaitingMinutes;
        zone.WaitingPerMinute = r.WaitingPerMinute;
        zone.SortOrder = r.SortOrder ?? 0;
        zone.IsActive = r.IsActive ?? true;
    }

    private async Task<IReadOnlyList<AdminAirportDto>> ToDtosAsync(IReadOnlyList<Airport> airports, CancellationToken ct)
    {
        var ids = airports.Select(a => a.Id).ToList();
        var zoneCounts = await db.AirportZones.AsNoTracking().Where(z => ids.Contains(z.AirportId)).GroupBy(z => z.AirportId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var queueSizes = await db.AirportQueueEntries.AsNoTracking().Where(e => ids.Contains(e.AirportId) && (e.Status == AirportQueueStatus.Waiting || e.Status == AirportQueueStatus.Offered))
            .GroupBy(e => e.AirportId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return airports.Select(a => new AdminAirportDto(
            a.Id, a.CityId, a.Code, a.NameAr, a.NameEn, a.Lat, a.Lng, JsonSerializer.Deserialize<JsonElement>(a.Geofence), a.RequiresPickupZone, a.DefaultFreeWaitingMinutes,
            a.DefaultWaitingPerMinute, a.QueueEnabled, a.IsActive, zoneCounts.GetValueOrDefault(a.Id), queueSizes.GetValueOrDefault(a.Id), a.CreatedAt, a.UpdatedAt)).ToList();
    }

    private static AdminAirportZoneDto ToDto(AirportZone z) => new(
        z.Id, z.AirportId, z.Kind, z.Code, z.TerminalCode, z.NameAr, z.NameEn, z.Polygon is null ? null : JsonSerializer.Deserialize<JsonElement>(z.Polygon), z.Lat, z.Lng,
        z.InstructionsAr, z.InstructionsEn, z.FreeWaitingMinutes, z.WaitingPerMinute, z.SortOrder, z.IsActive, z.CreatedAt, z.UpdatedAt);

    private static object Snapshot(Airport a) => new
    {
        a.CityId, a.Code, a.NameAr, a.NameEn, a.Lat, a.Lng, a.Geofence, a.RequiresPickupZone, a.DefaultFreeWaitingMinutes, a.DefaultWaitingPerMinute, a.QueueEnabled, a.IsActive,
    };

    private static object ZoneSnapshot(AirportZone z) => new
    {
        z.AirportId, z.Kind, z.Code, z.TerminalCode, z.NameAr, z.NameEn, z.Polygon, z.Lat, z.Lng, z.InstructionsAr, z.InstructionsEn, z.FreeWaitingMinutes, z.WaitingPerMinute, z.SortOrder,
        z.IsActive,
    };
}
