using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Abstractions;

/// <summary>Клиент REST API Яндекс.Диска.</summary>
public interface IYandexDiskClient
{
    /// <summary>Метаинформация о ресурсе и его состоянии (тип, отпечаток содержимого, файлы).</summary>
    public Task<YandexDiskState> GetStateAsync(string accessToken, string path, CancellationToken ct = default);

    /// <summary>Содержимое папки для выбора источника в UI.</summary>
    public Task<YandexDiskFolderListing> GetFolderAsync(string accessToken, string path, int offset, int limit,
        CancellationToken ct = default);

    /// <summary>Скачивает содержимое ресурса (файл или папку в виде zip) в <paramref name="target"/>.</summary>
    public Task DownloadAsync(string accessToken, YandexDiskState state, Stream target,
        CancellationToken ct = default);
}
