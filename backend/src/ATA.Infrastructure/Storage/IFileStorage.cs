namespace ATA.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Persists the stream under <paramref name="key"/> (a relative, slash-separated path).</summary>
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
