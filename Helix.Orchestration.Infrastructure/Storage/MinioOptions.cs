namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// Configuration options for MinIO object storage.
/// </summary>
/// <param name="Endpoint">MinIO server endpoint (e.g., "localhost:9000").</param>
/// <param name="AccessKey">Access key for authentication.</param>
/// <param name="SecretKey">Secret key for authentication.</param>
/// <param name="BucketName">The default bucket name to use.</param>
/// <param name="UseSsl">Whether to use SSL/TLS for connections.</param>
public sealed record MinioOptions(
    string Endpoint,
    string AccessKey,
    string SecretKey,
    string BucketName,
    bool UseSsl = false);
