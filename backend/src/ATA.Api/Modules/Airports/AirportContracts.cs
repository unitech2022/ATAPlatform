using ATA.Domain.Airports;

namespace ATA.Api.Modules.Airports;

public sealed class AirportOptions
{
    public const string Section = "Airport";
    public const string MoveToBack = "move_to_back";
    public const string Remove = "remove";

    /// <summary>A driver outside the waiting area (or offline) for longer than this leaves the queue.</summary>
    public int QueueExitGraceSeconds { get; set; } = 180;
    /// <summary>What happens to a queued driver who rejects / lets an offer expire: <c>move_to_back</c> (<c>entered_at = now</c>) or <c>remove</c>.</summary>
    public string RejectAction { get; set; } = MoveToBack;
    /// <summary>Queue offers per trip before the normal matching (F9) takes over.</summary>
    public int QueueMaxOffers { get; set; } = 5;
    /// <summary>Runs <c>AirportQueueJob</c> (every <see cref="JobIntervalSeconds"/>); <c>false</c> in tests, which call it directly.</summary>
    public bool JobsEnabled { get; set; } = true;
    public int JobIntervalSeconds { get; set; } = 30;

    public bool RemovesOnReject => string.Equals(RejectAction, Remove, StringComparison.OrdinalIgnoreCase);
}

// ----- catalog / passenger -----

public sealed record AirportRefDto(Guid Id, string Code, string Name);

public sealed record AirportTerminalDto(Guid Id, string Code, string? TerminalCode, string Name);

public sealed record AirportPickupZoneDto(Guid Id, string Code, string? TerminalCode, string Name, decimal Lat, decimal Lng, string? Instructions, int? FreeWaitingMinutes);

public sealed record AirportCatalogDto(
    Guid Id, string Code, string Name, decimal Lat, decimal Lng, IReadOnlyList<AirportTerminalDto> Terminals, IReadOnlyList<AirportPickupZoneDto> PickupZones);

public sealed record AirportResolveDto(AirportRefDto Airport, bool RequiresPickupZone, IReadOnlyList<AirportPickupZoneDto> PickupZones, IReadOnlyList<AirportTerminalDto> Terminals);

/// <summary><c>Trip.airport</c> / <c>Offer.airport</c> (the offer copy never carries the flight number).</summary>
public sealed record TripAirportDto(string Code, AirportDirection Direction, string? ZoneName, string? TerminalCode, string? FlightNumber, int? FreeWaitingMinutes);

// ----- driver queue -----

public sealed record AirportQueueStatusDto(
    bool InQueue, AirportRefDto? Airport = null, int? Position = null, int? Total = null, DateTime? EnteredAt = null, int? EstimatedWaitMinutes = null, AirportRefDto? EligibleAirport = null);

public sealed record AirportQueueJoinRequest(decimal? Lat, decimal? Lng);

/// <summary>SignalR <c>AirportQueueUpdated</c> (to the driver).</summary>
public sealed record AirportQueueUpdatedEvent(int Position, int Total, int? EstimatedWaitMinutes);

// ----- admin -----

public sealed record AdminAirportDto(
    Guid Id, Guid CityId, string Code, string NameAr, string NameEn, decimal Lat, decimal Lng, System.Text.Json.JsonElement Geofence, bool RequiresPickupZone,
    int? DefaultFreeWaitingMinutes, decimal? DefaultWaitingPerMinute, bool QueueEnabled, bool IsActive, int ZonesCount, int QueueSize, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AirportUpsertRequest(
    Guid? CityId, string? Code, string? NameAr, string? NameEn, decimal? Lat, decimal? Lng, System.Text.Json.JsonElement? Geofence, bool? RequiresPickupZone,
    int? DefaultFreeWaitingMinutes, decimal? DefaultWaitingPerMinute, bool? QueueEnabled, bool? IsActive);

public sealed record AdminAirportZoneDto(
    Guid Id, Guid AirportId, AirportZoneKind Kind, string Code, string? TerminalCode, string NameAr, string NameEn, System.Text.Json.JsonElement? Polygon, decimal Lat, decimal Lng,
    string? InstructionsAr, string? InstructionsEn, int? FreeWaitingMinutes, decimal? WaitingPerMinute, int SortOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record AirportZoneUpsertRequest(
    AirportZoneKind? Kind, string? Code, string? TerminalCode, string? NameAr, string? NameEn, System.Text.Json.JsonElement? Polygon, decimal? Lat, decimal? Lng,
    string? InstructionsAr, string? InstructionsEn, int? FreeWaitingMinutes, decimal? WaitingPerMinute, int? SortOrder, bool? IsActive);

public sealed record AdminAirportQueueEntryDto(
    Guid EntryId, int Position, Guid DriverId, string? DriverName, string? CategoryCode, DateTime EnteredAt, DateTime LastSeenAt, AirportQueueStatus Status);

public sealed record RemoveQueueEntryRequest(string? Reason);
