namespace ATA.Domain.Trips;

public class TripStop
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TripId { get; set; }
    public byte Sequence { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public DateTime? ArrivedAt { get; set; }
}
