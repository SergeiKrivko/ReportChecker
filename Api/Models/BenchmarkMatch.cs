namespace ReportChecker.Models;

/// <summary>
/// Запрос на LLM-сопоставление оставшихся найденных ошибок с известными.
/// </summary>
public class BenchmarkMatchRequest
{
    public required BenchmarkExpectedIssue[] Expected { get; init; }

    public required BenchmarkFoundIssue[] Found { get; init; }
}

/// <summary>
/// Пара «известная ошибка — найденная ошибка», предложенная моделью.
/// </summary>
public class BenchmarkMatch
{
    public required int ExpectedNumber { get; init; }

    public required int FoundIndex { get; init; }

    /// <summary>Уверенность модели в сопоставлении, 0–100.</summary>
    public int ConfidencePercent { get; init; }

    public string? Reason { get; init; }
}

/// <summary>
/// Результат LLM-сопоставления.
/// </summary>
public class BenchmarkMatchResult
{
    public BenchmarkMatch[] Matches { get; init; } = [];

    public BenchmarkUsage Usage { get; init; } = new();
}
