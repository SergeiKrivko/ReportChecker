namespace ReportChecker.Api.Dtos;

public class GoogleDriveReportSourceDto
{
    /// <summary>Идентификатор файла или папки в Google Drive.</summary>
    public required string FileId { get; init; }

    /// <summary>Имя файла или папки — для названия отчёта и отображения.</summary>
    public required string Name { get; init; }

    /// <summary>Файл входа внутри папки или архива.</summary>
    public string? EntryFilePath { get; init; }
}
