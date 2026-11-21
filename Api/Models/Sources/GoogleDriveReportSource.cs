namespace ReportChecker.Models.Sources;

public class GoogleDriveReportSource
{
    /// <summary>Владелец Диска: по нему берётся OAuth-токен.</summary>
    public required Guid OwnerId { get; init; }

    /// <summary>Идентификатор файла или папки в Google Drive (для корня — <c>root</c>).</summary>
    public required string FileId { get; init; }

    /// <summary>Имя файла или папки — используется для названия отчёта и отображения.</summary>
    public required string Name { get; init; }

    /// <summary>Файл входа внутри папки (для архивов и папок).</summary>
    public string? EntryFilePath { get; init; }
}
