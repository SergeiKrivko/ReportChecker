using ReportChecker.Models.GoogleDrive;
using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

/// <summary>
/// Снимает снимок версии с Google Диска и складывает его в S3: дальше проверка
/// работает с неизменяемым снимком, даже если файл на Диске поменяется.
/// </summary>
public interface IGoogleDriveSnapshotService
{
    public Task<CheckSource<GoogleDriveCheckSource>> CreateSnapshotAsync(Guid ownerId, string fileId,
        GoogleDriveState state, Guid? checkId = null, CancellationToken ct = default);
}
