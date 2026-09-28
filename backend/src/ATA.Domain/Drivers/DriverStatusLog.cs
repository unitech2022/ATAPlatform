using ATA.Domain.Common;

namespace ATA.Domain.Drivers;

public class DriverStatusLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid DriverId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime ChangedAt { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
