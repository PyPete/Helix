using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Helix.Orchestration.Infrastructure.Storage;

public interface IObjectStorageSink
{
    Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken cancellationToken = default);

    Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default);

    Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a stream to object storage using a compound key (bucket encoded in path or configured default).
    /// Key format: "{experimentId}/{trendId}/{timestamp}.ndjson"
    /// </summary>
    Task WriteAsync(string key, Stream stream, string contentType, CancellationToken cancellationToken = default);
}
