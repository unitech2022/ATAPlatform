using ATA.Domain.Common;

namespace ATA.Domain.Airports;

/// <summary><c>trips.airport_direction</c>: the pickup or the dropoff of the trip lies inside an airport geofence.</summary>
public enum AirportDirection { Pickup, Dropoff }

public enum AirportZoneKind { Terminal, PickupZone, DriverWaitingArea }

public enum AirportQueueStatus { Waiting, Offered, Dispatched, Left, Removed }

public enum AirportQueueLeftReason { ExitedArea, Offline, TripAssigned, RejectedOffer, AdminRemoved }

/// <summary>Row of <c>airports</c> (doc 11 §F17.6). <see cref="Geofence"/> is a <c>[[lat,lng],…]</c> ring.</summary>
public class Airport : AuditableEntity
{
    public Guid CityId { get; set; }
    /// <summary>IATA code, unique (<c>RUH</c>).</summary>
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public required string Geofence { get; set; }
    public bool RequiresPickupZone { get; set; } = true;
    public int? DefaultFreeWaitingMinutes { get; set; }
    public decimal? DefaultWaitingPerMinute { get; set; }
    public bool QueueEnabled { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Row of <c>airport_zones</c>: a terminal, a rider pickup zone or a driver waiting area (polygon required). UNIQUE(airport_id, code).</summary>
public class AirportZone : AuditableEntity
{
    public Guid AirportId { get; set; }
    public AirportZoneKind Kind { get; set; }
    public required string Code { get; set; }
    /// <summary><c>T1</c>…<c>T5</c>; a pickup zone points at its terminal.</summary>
    public string? TerminalCode { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? Polygon { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public string? InstructionsAr { get; set; }
    public string? InstructionsEn { get; set; }
    public int? FreeWaitingMinutes { get; set; }
    public decimal? WaitingPerMinute { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Row of <c>airport_queue_entries</c>: a driver in the FIFO queue of an airport (one active entry per driver).</summary>
public class AirportQueueEntry : Entity
{
    public Guid AirportId { get; set; }
    public Guid DriverId { get; set; }
    public Guid RideCategoryId { get; set; }
    public AirportQueueStatus Status { get; set; } = AirportQueueStatus.Waiting;
    public DateTime EnteredAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public Guid? OfferedTripId { get; set; }
    public DateTime? LeftAt { get; set; }
    public AirportQueueLeftReason? LeftReason { get; set; }

    public bool IsActive => Status is AirportQueueStatus.Waiting or AirportQueueStatus.Offered;

    public static readonly AirportQueueStatus[] ActiveStatuses = [AirportQueueStatus.Waiting, AirportQueueStatus.Offered];
}
