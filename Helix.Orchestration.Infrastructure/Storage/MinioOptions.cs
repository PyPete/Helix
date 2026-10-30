namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// Configuration options for S3-compatible object storage (MinIO, AWS S3, Wasabi, etc.).
/// </summary>
public class MinioOptions
{
    /// <summary>
    /// Service URL for S3-compatible storage (e.g., "https://play.min.io" for MinIO, or "https://s3.amazonaws.com" for AWS).
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Access key (username) for authentication.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key (password) for authentication.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region (e.g., "us-east-1"). Required for AWS S3, optional for MinIO.
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Default bucket name for experiment data storage.
    /// </summary>
    public string DefaultBucket { get; set; } = "helix-experiments";

    /// <summary>
    /// Force path-style bucket addressing (required for MinIO, optional for S3).
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;
}

