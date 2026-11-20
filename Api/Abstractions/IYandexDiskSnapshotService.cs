using ReportChecker.Models.Sources;
using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Abstractions;

/// <summary>
/// Снимает снимок версии с Диска и складывает его в S3: дальше проверка работает
/// с неизменяемым снимком, даже если файл на Диске поменяется.
/// </summary>
public interface IYandexDiskSnapshotService
{
    public Task<CheckSource<YandexDiskCheckSource>> CreateSnapshotAsync(Guid ownerId, string path,
        YandexDiskState state, Guid? checkId = null, CancellationToken ct = default);
}
