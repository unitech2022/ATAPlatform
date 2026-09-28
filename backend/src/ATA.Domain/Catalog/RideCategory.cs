using ATA.Domain.Common;

namespace ATA.Domain.Catalog;

public class RideCategory : AuditableEntity
{
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Icon { get; set; }
    public byte Seats { get; set; } = 4;
    public byte MaxStops { get; set; } = 2;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // FlatPricing inputs (F8); replaced by pricing rules in F10.
    public decimal BaseFare { get; set; }
    public decimal PerKm { get; set; }
    public decimal PerMinute { get; set; }
    public decimal BookingFee { get; set; }
    public decimal MinFare { get; set; }
    /// <summary>Share of the fare paid out to the driver, in percent (default 80).</summary>
    public decimal DriverSharePercent { get; set; } = 80m;
}
