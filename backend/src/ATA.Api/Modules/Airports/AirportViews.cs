using ATA.Domain.Airports;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Airports;

/// <summary>Builds <c>Trip.airport</c> / <c>Offer.airport</c> from the columns stored on the trip (also for airports or zones that were deactivated later).</summary>
public sealed class AirportViewBuilder(AtaDbContext db)
{
    public async Task<TripAirportDto?> BuildAsync(Trip trip, Language lang, bool includeFlightNumber, CancellationToken ct)
    {
        if (trip.AirportId is not { } airportId || trip.AirportDirection is not { } direction)
        {
            return null;
        }

        var code = await db.Airports.AsNoTracking().Where(a => a.Id == airportId).Select(a => a.Code).FirstOrDefaultAsync(ct);
        if (code is null)
        {
            return null;
        }

        var zoneName = trip.AirportZoneId is { } zoneId
            ? await db.AirportZones.AsNoTracking().Where(z => z.Id == zoneId).Select(z => lang == Language.En ? z.NameEn : z.NameAr).FirstOrDefaultAsync(ct)
            : null;
        return new TripAirportDto(code, direction, zoneName, trip.TerminalCode, includeFlightNumber ? trip.FlightNumber : null, WaitingPolicy.Parse(trip.WaitingPolicy)?.FreeMinutes);
    }
}
