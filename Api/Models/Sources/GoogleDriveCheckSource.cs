namespace ReportChecker.Models.Sources;

public class GoogleDriveCheckSource
{
    /// <summary>Идентификатор файла или папки, с которых снят снимок версии.</summary>
    public required string FileId { get; init; }

    /// <summary>
    /// Отпечаток содержимого на момент снятия снимка: md5 файла или sha256
    /// отсортированного списка файлов папки. По нему определяется, изменился ли
    /// отчёт на Диске, чтобы не создавать проверки на каждое unrelated-событие.
    /// </summary>
    public required string ContentHash { get; init; }

    /// <summary>Имя объекта в S3: имя файла или <c>sources.zip</c> для папки.</summary>
    public string? FileName { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
