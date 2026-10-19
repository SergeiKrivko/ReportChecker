namespace ReportChecker.Models;

/// <summary>
/// Строка главы теста бенчмарка.
/// </summary>
public class BenchmarkCaseLine
{
    /// <summary>Номер строки внутри главы, с единицы — тот же, что и в <c>case.json</c>.</summary>
    public required int Number { get; init; }

    public required string Content { get; init; }
}
