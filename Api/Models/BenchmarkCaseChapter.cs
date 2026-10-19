namespace ReportChecker.Models;

/// <summary>
/// Глава теста бенчмарка с пронумерованными строками.
/// </summary>
public class BenchmarkCaseChapter
{
    /// <summary>Имя главы в том же виде, что и в <c>case.json</c> (например, <c>&lt;root&gt;//Введение</c>).</summary>
    public required string Name { get; init; }

    public BenchmarkCaseLine[] Lines { get; init; } = [];
}
