namespace ATA.Domain.Trips;

/// <summary>Latest known position per driver (<c>driver_locations</c>, keyed by driver id).</summary>
public class DriverLocation
{
    public Guid DriverId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public decimal? Heading { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Accuracy { get; set; }
    public bool IsOnline { get; set; }
    public Guid? CurrentTripId { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DriverLocationHistory
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DriverId { get; set; }
    public Guid? TripId { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public DateTime RecordedAt { get; set; }
}
