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
}
