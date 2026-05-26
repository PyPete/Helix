namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// Defines the contract for writing data to object storage (S3, Blob, MinIO, etc.).
/// </summary>
public interface IObjectStorageSink
{
    /// <summary>
    /// Writes data from a stream to object storage at the specified key.
    /// </summary>
    /// <param name="objectKey">The unique key/path for the object in storage.</param>
    /// <param name="data">The data stream to write.</param>
    /// <param name="contentType">The MIME type of the content (e.g., "application/json").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteAsync(string objectKey, Stream data, string contentType, CancellationToken ct);
}
