using System.Diagnostics;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;

namespace ReportChecker.S3;

public class S3Repository(ILogger<S3Repository> logger) : IFileRepository
{
    private readonly AmazonS3Client _s3Client = new(
        new BasicAWSCredentials(Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
            Environment.GetEnvironmentVariable("S3_SECRET_KEY")),
        new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_SERVICE_URL"),
            AuthenticationRegion = Environment.GetEnvironmentVariable("S3_AUTHORIZATION_REGION"),
            ForcePathStyle = true,
            Timeout = TimeSpan.FromSeconds(2),
            RetryMode = RequestRetryMode.Standard,
            MaxErrorRetry = 0,
            ConnectTimeout = TimeSpan.FromSeconds(2),
        });

    private readonly AmazonS3Client _retryS3Client = new(
        new BasicAWSCredentials(Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
            Environment.GetEnvironmentVariable("S3_SECRET_KEY")),
        new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_SERVICE_URL"),
            AuthenticationRegion = Environment.GetEnvironmentVariable("S3_AUTHORIZATION_REGION"),
            ForcePathStyle = true,
            Timeout = TimeSpan.FromSeconds(15),
            RetryMode = RequestRetryMode.Standard,
            MaxErrorRetry = 3,
            ConnectTimeout = TimeSpan.FromSeconds(2),
        });

    private async Task<TResult> RetryRequest<TResult>(Func<AmazonS3Client, Task<TResult>> action)
    {
        try
        {
            return await action(_s3Client);
        }
        catch (TimeoutException)
        {
            return await action(_retryS3Client);
        }
    }

    public Task<Stream> DownloadFileAsync(FileRepositoryBucket bucket, Guid fileId)
    {
        return DownloadFileAsync(GetBucket(bucket), fileId.ToString());
    }

    public Task<Stream> DownloadFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName)
    {
        return DownloadFileAsync(GetBucket(bucket), $"{fileId.ToString()}/{fileName}");
    }

    public Task DeleteFileAsync(FileRepositoryBucket bucket, Guid fileId)
    {
        return DeleteFileAsync(GetBucket(bucket), fileId.ToString());
    }

    public Task DeleteFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName)
    {
        return DeleteFileAsync(GetBucket(bucket), $"{fileId.ToString()}/{fileName}");
    }

    public Task<bool> FileExistsAsync(FileRepositoryBucket bucket, Guid fileId)
    {
        return FileExistsAsync(GetBucket(bucket), fileId.ToString());
    }

    public Task<bool> FileExistsAsync(FileRepositoryBucket bucket, Guid fileId, string fileName)
    {
        return FileExistsAsync(GetBucket(bucket), $"{fileId.ToString()}/{fileName}");
    }

    public Task UploadFileAsync(FileRepositoryBucket bucket, Guid fileId, Stream fileStream)
    {
        return UploadFileAsync(GetBucket(bucket), fileId.ToString(), fileStream);
    }

    public Task UploadFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName, Stream fileStream)
    {
        return UploadFileAsync(GetBucket(bucket), $"{fileId.ToString()}/{fileName}", fileStream);
    }

    public Task<string> GetDownloadUrlAsync(FileRepositoryBucket bucket, Guid fileId, TimeSpan timeout)
    {
        return GetDownloadUrlAsync(GetBucket(bucket), fileId.ToString(), timeout);
    }

    public async Task<string> GetDownloadUrlAsync(FileRepositoryBucket bucket, Guid fileId, string fileName,
        TimeSpan timeout)
    {
        if (!await FileExistsAsync(bucket, fileId, fileName))
            return await GetDownloadUrlAsync(GetBucket(bucket), $"{fileId}{Path.GetExtension(fileName)}", timeout);
        return await GetDownloadUrlAsync(GetBucket(bucket), $"{fileId.ToString()}/{fileName}", timeout);
    }

    private async Task<Stream> DownloadFileAsync(string bucket, string fileName)
    {
        var stopwatch = Stopwatch.StartNew();
        var stream = (await RetryRequest(client => client.GetObjectAsync(bucket, fileName)))
            .ResponseStream;
        stopwatch.Stop();
        logger.LogInformation("Object '{name}' downloaded from '{bucket}' in {time}.'", fileName, bucket,
            stopwatch.Elapsed);
        return stream;
    }

    private async Task DeleteFileAsync(string bucket, string fileName)
    {
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = fileName,
        };

        var stopwatch = Stopwatch.StartNew();
        await RetryRequest(client => client.DeleteObjectAsync(deleteRequest));
        stopwatch.Stop();
        logger.LogInformation("Object '{name}' deleted from '{bucket}' in {time}.'", fileName, bucket,
            stopwatch.Elapsed);
    }

    private async Task<bool> FileExistsAsync(string bucket, string fileName)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = fileName,
            };

            await RetryRequest(client => client.GetObjectMetadataAsync(request));
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task UploadFileAsync(string bucket, string fileName, Stream fileStream)
    {
        var putRequest = new PutObjectRequest
        {
            BucketName = bucket,
            Key = fileName,
            InputStream = fileStream,
            ContentType = "application/octet-stream"
        };

        var stopwatch = Stopwatch.StartNew();
        await _retryS3Client.PutObjectAsync(putRequest);
        stopwatch.Stop();
        logger.LogInformation("Object '{name}' uploaded to '{bucket}' in {time}.'", fileName, bucket,
            stopwatch.Elapsed);
    }

    private async Task<string> GetDownloadUrlAsync(string bucket, string fileName, TimeSpan timeout)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = fileName,
            Expires = DateTime.UtcNow.Add(timeout)
        };
        var stopwatch = Stopwatch.StartNew();
        var url = await RetryRequest(client => client.GetPreSignedURLAsync(request));
        stopwatch.Stop();
        logger.LogInformation("Url for object '{name}' in '{bucket}' get in {time}", fileName, bucket,
            stopwatch.Elapsed);
        return url;
    }

    public async Task<int> ClearFilesCreatedBefore(FileRepositoryBucket bucket, DateTime beforeDate,
        CancellationToken cancellationToken)
    {
        var count = 0;
        var bucketName = GetBucket(bucket);
        var files = await RetryRequest(client => client.ListObjectsAsync(bucketName, cancellationToken));
        foreach (var file in files?.S3Objects ?? [])
        {
            if (file != null && file.LastModified < beforeDate)
            {
                await DeleteFileAsync(bucketName, file.Key);
                count++;
            }
        }

        return count;
    }

    private static string SourcesBucket { get; } = Environment.GetEnvironmentVariable("S3_SOURCES_BUCKET") ?? "sources";

    private static string GetBucket(FileRepositoryBucket bucket)
    {
        return bucket switch
        {
            FileRepositoryBucket.Sources => SourcesBucket,
            _ => throw new ArgumentOutOfRangeException(nameof(bucket), bucket, null)
        };
    }
}