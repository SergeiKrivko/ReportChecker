namespace ReportChecker.Models;

/// <summary>
/// Тест (кейс) бенчмарка: текст, в котором нужно найти ошибки, и список известных ошибок.
/// </summary>
public class BenchmarkCase
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Ключ провайдера формата (например, <c>Latex</c>).</summary>
    public required string Format { get; init; }

    /// <summary>Имя файла-точки входа относительно каталога теста.</summary>
    public string? EntryFile { get; init; }

    public string? Description { get; init; }

    /// <summary>Режим показа теста на фронте.</summary>
    public BenchmarkCaseDisplayMode DisplayMode { get; init; } = BenchmarkCaseDisplayMode.Collapsed;

    public BenchmarkExpectedIssue[] Expected { get; init; } = [];

    /// <summary>Описание проблемы с загрузкой теста, <c>null</c> если тест корректен.</summary>
    public string? ValidationError { get; init; }
}
