namespace ReportChecker.Api.Dtos;

public class YandexDiskReportSourceDto
{
    /// <summary>Путь на Диске, например <c>disk:/Отчёты/report.tex</c>.</summary>
    public required string Path { get; init; }

    /// <summary>Файл входа внутри папки или архива.</summary>
    public string? EntryFilePath { get; init; }
}
