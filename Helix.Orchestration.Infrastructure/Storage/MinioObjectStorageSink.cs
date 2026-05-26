using Minio;
using Minio.DataModel.Args;

namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// MinIO implementation of <see cref="IObjectStorageSink"/>.
/// Writes objects to a MinIO (S3-compatible) bucket.
/// </summary>
public sealed class MinioObjectStorageSink : IObjectStorageSink
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private bool _bucketChecked;
    private readonly SemaphoreSlim _bucketCheckLock = new(1, 1);

    public MinioObjectStorageSink(MinioOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _bucketName = options.BucketName ?? throw new ArgumentNullException(nameof(options.BucketName));

        // Build MinIO client with provided options
        var builder = new MinioClient()
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey);

        if (options.UseSsl)
        {
            builder = builder.WithSSL();
        }

        _minioClient = builder.Build();
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string objectKey, Stream data, string contentType, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(data);

        // Ensure bucket exists (check once per instance lifetime)
        await EnsureBucketExistsAsync(ct);

        // Upload object to MinIO
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(data.Length)
            .WithContentType(contentType);

        await _minioClient.PutObjectAsync(putArgs, ct);
    }

    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        if (_bucketChecked)
            return;

        await _bucketCheckLock.WaitAsync(ct);
        try
        {
            if (_bucketChecked)
                return;

            var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, ct);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(makeBucketArgs, ct);
            }

            _bucketChecked = true;
        }
        finally
        {
            _bucketCheckLock.Release();
        }
    }
}
