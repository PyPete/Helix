using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace Helix.Orchestration.Infrastructure.Storage;

public class MinioObjectStorageSink : IObjectStorageSink
{
    private readonly IAmazonS3 _client;
    private readonly MinioOptions _options;

    public MinioObjectStorageSink(MinioOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ServiceUrl)) throw new ArgumentException("Service URL must be configured.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.AccessKey)) throw new ArgumentException("Access key must be configured.", nameof(options));
        if (string.IsNullOrWhiteSpace(options.SecretKey)) throw new ArgumentException("Secret key must be configured.", nameof(options));

        _options = options;

        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = options.ForcePathStyle
        };

        if (!string.IsNullOrWhiteSpace(options.Region))
            config.AuthenticationRegion = options.Region;

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        _client = new AmazonS3Client(credentials, config);
    }

    public async Task UploadObjectAsync(string bucketName, string objectName, Stream stream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("Object name required.", nameof(objectName));
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        try { await _client.PutBucketAsync(bucketName, cancellationToken); }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectName,
            InputStream = stream
        }, cancellationToken);
    }

    public async Task DownloadObjectAsync(string bucketName, string objectName, Stream outputStream, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("Object name required.", nameof(objectName));
        if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));

        using var response = await _client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        }, cancellationToken);
        await response.ResponseStream.CopyToAsync(outputStream, cancellationToken);
    }

    public async Task<bool> ObjectExistsAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("Object name required.", nameof(objectName));

        try
        {
            await _client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectName
            }, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bucketName)) throw new ArgumentException("Bucket name required.", nameof(bucketName));
        if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("Object name required.", nameof(objectName));

        await _client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectName
        }, cancellationToken);
    }

    public async Task WriteAsync(string key, Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key required.", nameof(key));
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        var bucket = _options.DefaultBucket ?? "helix-recordings";

        try { await _client.PutBucketAsync(bucket, cancellationToken); }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict) { }

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        }, cancellationToken);
    }
}
