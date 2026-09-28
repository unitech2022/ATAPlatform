using ATA.Domain.Common;

namespace ATA.Domain.Drivers;

public class Vehicle : AuditableEntity
{
    public Guid DriverId { get; set; }
    public Guid RideCategoryId { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public short Year { get; set; }
    public required string Color { get; set; }
    public required string PlateNumber { get; set; }
    public byte Seats { get; set; }
    public bool IsActive { get; set; } = true;
}
