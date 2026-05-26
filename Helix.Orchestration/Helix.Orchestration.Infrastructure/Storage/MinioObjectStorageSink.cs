using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace Helix.Orchestration.Infrastructure.Storage;

/// <summary>
/// S3-compatible object storage implementation supporting MinIO, AWS S3, Wasabi, and other S3-compatible services.
/// </summary>
public class MinioObjectStorageSink : IObjectStorageSink
{
    private readonly IAmazonS3 _client;
    private readonly MinioOptions _options;

    public MinioObjectStorageSink(MinioOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(options.ServiceUrl))
            throw new ArgumentException("Service URL must be configured.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.AccessKey))
            throw new ArgumentException("Access key must be configured.", nameof(options));

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new ArgumentException("Secret key must be configured.", nameof(options));

        _options = options;

        // Configure S3 client for S3-compatible storage
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = options.ForcePathStyle
        };

        // If a region is specified, use it
        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            config.AuthenticationRegion = options.Region;
        }

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    /// <inheritdoc/>
    public async Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name must be specified.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("Object name must be specified.", nameof(objectName));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        // Ensure bucket exists
        try
        {
            await _client.PutBucketAsync(bucketName, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Bucket already exists, continue
        }

        // Upload the object
        var putRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            InputStream = stream
        };

        await _client.PutObjectAsync(putRequest, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name must be specified.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("Object name must be specified.", nameof(objectName));

        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));

        var getRequest = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        };

        using var response = await _client.GetObjectAsync(getRequest, cancellationToken);
        await response.ResponseStream.CopyToAsync(outputStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name must be specified.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("Object name must be specified.", nameof(objectName));

        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectName
            };

            await _client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Bucket name must be specified.", nameof(bucketName));

        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("Object name must be specified.", nameof(objectName));

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        };

        await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
    }
}
