using ATA.Domain.Common;

namespace ATA.Domain.Catalog;

public enum DocumentAppliesTo { Driver, Vehicle }

public class DocumentType : Entity
{
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public DocumentAppliesTo AppliesTo { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool RequiresExpiry { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
