using ATA.Domain.Common;

namespace ATA.Domain.Files;

public class StoredFile : Entity
{
    public Guid OwnerUserId { get; set; }
    public required string StorageKey { get; set; }
    public required string OriginalName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string Sha256 { get; set; }
}
