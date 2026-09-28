using ATA.Domain.Common;

namespace ATA.Domain.Passengers;

public enum SavedPlaceLabel { Home, Work, Other }

public class SavedPlace : Entity
{
    public Guid PassengerId { get; set; }
    public SavedPlaceLabel Label { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}
