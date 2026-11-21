using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models.GoogleDrive;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.GoogleDrive;

public class GoogleDriveSnapshotService(
    IGoogleDriveClient client,
    IGoogleDriveTokenService tokenService,
    ICheckSourceRepository<GoogleDriveCheckSource> checkSourceRepository,
    IFileRepository fileRepository,
    IConfiguration configuration,
    ILogger<GoogleDriveSnapshotService> logger) : IGoogleDriveSnapshotService
{
    private const long DefaultMaxBytes = 200L * 1024 * 1024;

    public async Task<CheckSource<GoogleDriveCheckSource>> CreateSnapshotAsync(Guid ownerId, string fileId,
        GoogleDriveState state, Guid? checkId = null, CancellationToken ct = default)
    {
        var maxBytes = long.TryParse(configuration["GoogleDrive:MaxSnapshotBytes"], out var configured)
            ? configured
            : DefaultMaxBytes;
        var size = state.Files.Sum(e => e.Size);
        if (size > maxBytes)
            throw new BadRequestException(
                $"Отчёт на Google Диске занимает {size / (1024 * 1024)} МБ — больше допустимых " +
                $"{maxBytes / (1024 * 1024)} МБ. Уменьшите размер папки");

        var token = await tokenService.GetAccessTokenAsync(ownerId, ct);
        var fileName = state.IsFolder ? "sources.zip" : SafeFileName(state.Name);
        var tempPath = Path.Combine(Path.GetTempPath(), $"google-drive-{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var file = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                             81920, useAsync: true))
            {
                await client.DownloadAsync(token, state, file, ct);
            }

            var data = new GoogleDriveCheckSource
            {
                FileId = state.FileId,
                ContentHash = state.Hash,
                FileName = fileName,
                CreatedAt = DateTime.UtcNow,
            };
            var id = await checkSourceRepository.CreateAsync(checkId, data, ct);

            await using var upload = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                useAsync: true);
            await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, fileName, upload);

            logger.LogInformation(
                "Saved Google Drive snapshot of '{fileId}' ({size} bytes) as source '{id}'", fileId, size, id);

            return new CheckSource<GoogleDriveCheckSource>
            {
                Id = id,
                CheckId = checkId,
                Data = data,
            };
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to delete temporary snapshot file '{path}'", tempPath);
            }
        }
    }

    private static string SafeFileName(string name)
    {
        var sanitized = name.Replace('/', '_').Replace('\\', '_');
        if (sanitized.Length <= 200)
            return sanitized;

        var extension = Path.GetExtension(sanitized);
        return sanitized[..(200 - extension.Length)] + extension;
    }
}
