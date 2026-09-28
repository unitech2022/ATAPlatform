using ATA.Domain.Common;

namespace ATA.Domain.Catalog;

public class City : Entity
{
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string CountryCode { get; set; } = "SA";
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    public bool IsActive { get; set; } = true;
}
