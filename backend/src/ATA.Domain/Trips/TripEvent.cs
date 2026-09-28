using ATA.Domain.Common;

namespace ATA.Domain.Trips;

public class TripEvent : Entity
{
    public Guid TripId { get; set; }
    public required string Type { get; set; }
    public TripActor Actor { get; set; }
    public Guid? ActorUserId { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    /// <summary>JSON object with extra payload for the event.</summary>
    public string? Data { get; set; }
}
