using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// Abstraction for object storage operations (e.g., MinIO, S3).
/// </summary>
public interface IObjectStorageSink
{
    /// <summary>
    /// Uploads an object (file) to storage.
    /// </summary>
    /// <param name="bucketName">The bucket/container name.</param>
    /// <param name="objectName">The object key/path within the bucket.</param>
    /// <param name="stream">The data stream to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an object from storage.
    /// </summary>
    /// <param name="bucketName">The bucket/container name.</param>
    /// <param name="objectName">The object key/path within the bucket.</param>
    /// <param name="outputStream">The output stream to write data to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an object exists in storage.
    /// </summary>
    /// <param name="bucketName">The bucket/container name.</param>
    /// <param name="objectName">The object key/path within the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object from storage.
    /// </summary>
    /// <param name="bucketName">The bucket/container name.</param>
    /// <param name="objectName">The object key/path within the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);
}
