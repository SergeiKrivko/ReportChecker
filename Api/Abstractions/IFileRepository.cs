namespace ReportChecker.Abstractions;

public interface IFileRepository
{
    public Task<Stream> DownloadFileAsync(FileRepositoryBucket bucket, Guid fileId);
    public Task<Stream> DownloadFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName);
    public Task DeleteFileAsync(FileRepositoryBucket bucket, Guid fileId);
    public Task DeleteFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName);
    public Task<bool> FileExistsAsync(FileRepositoryBucket bucket, Guid fileId);
    public Task<bool> FileExistsAsync(FileRepositoryBucket bucket, Guid fileId, string fileName);
    public Task UploadFileAsync(FileRepositoryBucket bucket, Guid fileId, Stream fileStream);
    public Task UploadFileAsync(FileRepositoryBucket bucket, Guid fileId, string fileName, Stream fileStream);
    public Task<string> GetDownloadUrlAsync(FileRepositoryBucket bucket, Guid fileId, TimeSpan timeout);
    public Task<string> GetDownloadUrlAsync(FileRepositoryBucket bucket, Guid fileId, string fileName, TimeSpan timeout);
    public Task<int> ClearFilesCreatedBefore(FileRepositoryBucket bucket, DateTime beforeDate, CancellationToken cancellationToken);
}

public enum FileRepositoryBucket
{
    Sources
}