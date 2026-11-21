using ReportChecker.Models.GoogleDrive;

namespace ReportChecker.Abstractions;

/// <summary>Клиент REST API Google Drive (www.googleapis.com/drive/v3).</summary>
public interface IGoogleDriveClient
{
    /// <summary>Содержимое папки для выбора источника в UI.</summary>
    public Task<GoogleDriveFolderListing> GetFolderAsync(string accessToken, string folderId, string? pageToken,
        int pageSize, CancellationToken ct = default);

    /// <summary>Метаинформация о ресурсе и его состоянии (тип, отпечаток содержимого, файлы).</summary>
    public Task<GoogleDriveState> GetStateAsync(string accessToken, string fileId, CancellationToken ct = default);

    /// <summary>Скачивает содержимое ресурса (файл или папку в виде zip) в <paramref name="target"/>.</summary>
    public Task DownloadAsync(string accessToken, GoogleDriveState state, Stream target,
        CancellationToken ct = default);
}
