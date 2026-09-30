using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Airports;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Airports;

/// <summary>Flight numbers are stored only (doc 11 §F17.7): <c>^[A-Z0-9]{2}[0-9]{1,4}[A-Z]?$</c> after removing spaces and upper-casing.</summary>
public static partial class FlightNumbers
{
    [GeneratedRegex("^[A-Z0-9]{2}[0-9]{1,4}[A-Z]?$")]
    private static partial Regex Pattern();

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }

    public static bool IsValid(string normalized) => Pattern().IsMatch(normalized);
}

/// <summary>The airport client inputs of a quote or trip request.</summary>
public sealed record AirportRequestInput(Guid? PickupZoneId, string? TerminalCode, string? FlightNumber);

/// <summary>
/// The airport side of a trip after validation. <paramref name="PickupLat"/>/<paramref name="PickupLng"/> are the coordinates to price and dispatch from (the pickup zone's
/// point for an airport pickup, else the requested pickup).
/// </summary>
public sealed record AirportTripContext(
    AirportSnapshot Airport, AirportDirection Direction, AirportZoneSnapshot? PickupZone, string? TerminalCode, string? FlightNumber, decimal PickupLat, decimal PickupLng);

/// <summary>Airport detection and validation for quotes and trip requests (doc 11 §F17.7) and the waiting policy fixed at creation.</summary>
public sealed class AirportTripService(AirportCatalog catalog, IPricingService pricing)
{
    /// <summary><c>ride_categories.code</c> of the airport category: only available for trips that touch an airport.</summary>
    public const string AirportCategoryCode = "airport";

    /// <summary>
    /// Detects the airport (pickup first, then dropoff) and validates the airport fields. <paramref name="requirePickupZone"/> (trip requests) raises
    /// <c>422 airport_pickup_zone_required</c> for an airport pickup without a zone. Returns <c>null</c> for trips that do not touch an airport.
    /// </summary>
    public async Task<AirportTripContext?> PrepareAsync(
        decimal pickupLat, decimal pickupLng, decimal dropoffLat, decimal dropoffLng, AirportRequestInput input, bool requirePickupZone, CancellationToken ct)
    {
        var flight = FlightNumbers.Normalize(input.FlightNumber);
        var terminalCode = string.IsNullOrWhiteSpace(input.TerminalCode) ? null : input.TerminalCode.Trim().ToUpperInvariant();
        var validator = new Validator().Rule("flightNumber", flight is null || FlightNumbers.IsValid(flight), "invalid");
        validator.Rule("airportTerminalCode", terminalCode is null || terminalCode.Length <= 10, "invalid").ThrowIfInvalid();

        var all = await catalog.AllAsync(ct);
        var pickupAirport = await catalog.AtAsync(pickupLat, pickupLng, ct);
        var dropoffAirport = pickupAirport is null ? await catalog.AtAsync(dropoffLat, dropoffLng, ct) : null;

        AirportZoneSnapshot? zone = null;
        if (input.PickupZoneId is { } zoneId)
        {
            zone = all.SelectMany(a => a.Zones).FirstOrDefault(z => z.Id == zoneId && z.Kind == AirportZoneKind.PickupZone);
            validator.Rule("airportPickupZoneId", zone is not null, "unknown");
            validator.ThrowIfInvalid();
            validator.Rule("airportPickupZoneId", pickupAirport is not null && zone!.AirportId == pickupAirport.Id, "not_in_airport").ThrowIfInvalid();
        }

        var airport = pickupAirport ?? dropoffAirport;
        if (airport is null)
        {
            validator.Rule("airportTerminalCode", terminalCode is null, "unknown").ThrowIfInvalid();
            return null;
        }

        if (pickupAirport is not null)
        {
            if (zone is null && airport.RequiresPickupZone && requirePickupZone)
            {
                throw new DomainException(ErrorCodes.AirportPickupZoneRequired, new { airportId = airport.Id });
            }

            return new AirportTripContext(airport, AirportDirection.Pickup, zone, zone?.TerminalCode, flight,
                zone?.Lat ?? pickupLat, zone?.Lng ?? pickupLng);
        }

        if (terminalCode is not null)
        {
            validator.Rule("airportTerminalCode", airport.Terminals.Any(t => string.Equals(t.TerminalCode ?? t.Code, terminalCode, StringComparison.OrdinalIgnoreCase)), "unknown").ThrowIfInvalid();
        }

        return new AirportTripContext(airport, AirportDirection.Dropoff, null, terminalCode, flight, pickupLat, pickupLng);
    }

    /// <summary>
    /// <c>freeMinutes = zone ?? airport default ?? pricing rule</c> and <c>perMinute</c> in the same order, fixed on the trip; only airport pickups have a policy
    /// (the waiting happens at the airport).
    /// </summary>
    public async Task<WaitingPolicy?> WaitingPolicyAsync(AirportTripContext context, RideCategory category, DateTime at, CancellationToken ct)
    {
        if (context.Direction != AirportDirection.Pickup)
        {
            return null;
        }

        var point = new GeoPoint(context.PickupLat, context.PickupLng);
        var free = context.PickupZone?.FreeWaitingMinutes ?? context.Airport.DefaultFreeWaitingMinutes ?? await pricing.FreeWaitingMinutesAsync(category, point, at, ct);
        var perMinute = context.PickupZone?.WaitingPerMinute ?? context.Airport.DefaultWaitingPerMinute ?? await pricing.WaitingPerMinuteAsync(category, point, at, ct);
        return new WaitingPolicy(free, perMinute);
    }
}
