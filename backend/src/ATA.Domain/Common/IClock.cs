namespace ATA.Domain.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}
