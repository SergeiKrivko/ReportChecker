namespace ReportChecker.Models;

/// <summary>
/// Результат по одной ошибке в рамках прогона бенчмарка.
/// Строка создаётся как на каждую известную ошибку, так и на каждую «лишнюю», найденную моделью.
/// </summary>
public class BenchmarkResult
{
    public required Guid Id { get; init; }

    public required Guid RunId { get; init; }

    public DateTime CreatedAt { get; init; }

    /// <summary>Номер известной ошибки; <c>null</c> для «лишних» ошибок.</summary>
    public int? ExpectedNumber { get; init; }

    public string? ErrorClass { get; init; }

    public string? Chapter { get; init; }

    public int? Line { get; init; }

    public string? ExpectedTitle { get; init; }
    public string? ExpectedComment { get; init; }
    public int? ExpectedPriority { get; init; }

    /// <summary>Порядковый номер найденной ошибки; <c>null</c> если известная ошибка не найдена.</summary>
    public int? FoundIndex { get; init; }

    public string? FoundTitle { get; init; }
    public string? FoundComment { get; init; }
    public int? FoundPriority { get; init; }

    public bool IsFound { get; init; }

    /// <summary>Совпал ли заголовок (учитывается для найденных известных ошибок).</summary>
    public bool? TitleMatch { get; init; }

    /// <summary>Совпал ли приоритет (учитывается для найденных известных ошибок).</summary>
    public bool? PriorityMatch { get; init; }

    /// <summary>Разница приоритетов «найденный − эталонный».</summary>
    public int? PriorityDelta { get; init; }

    public BenchmarkMatchMethod MatchingMethod { get; init; }

    /// <summary>Оценка похожести заголовков, если применимо.</summary>
    public double? MatchScore { get; init; }

    public string? MatchingReason { get; init; }

    public BenchmarkFixMatchStatus FixMatchStatus { get; init; }

    public string? ExpectedFix { get; init; }
    public string? FoundFix { get; init; }
}
