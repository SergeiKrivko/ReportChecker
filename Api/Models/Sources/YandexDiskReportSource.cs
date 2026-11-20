namespace ReportChecker.Models.Sources;

public class YandexDiskReportSource
{
    /// <summary>Владелец Диска: по нему берётся OAuth-токен.</summary>
    public required Guid OwnerId { get; init; }

    /// <summary>Путь на Диске в формате API: <c>disk:/Отчёты/report.tex</c>.</summary>
    public required string Path { get; init; }

    /// <summary>Файл входа внутри папки (для архивов и папок).</summary>
    public string? EntryFilePath { get; init; }
}
