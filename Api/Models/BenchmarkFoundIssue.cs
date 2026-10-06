namespace ReportChecker.Models;

/// <summary>
/// Ошибка, найденная моделью в тексте теста бенчмарка.
/// </summary>
public class BenchmarkFoundIssue
{
    /// <summary>Порядковый номер найденной ошибки внутри прогона (начиная с 0).</summary>
    public int Index { get; init; }

    public required string Chapter { get; init; }

    public int? Line { get; init; }

    public required string Title { get; init; }

    public required string Comment { get; init; }

    public int Priority { get; init; } = 1;

    public PatchLine[]? Patch { get; init; }
}
