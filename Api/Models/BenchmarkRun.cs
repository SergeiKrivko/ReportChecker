namespace ReportChecker.Models;

/// <summary>
/// Прогон бенчмарка: одна пара (тест, модель).
/// </summary>
public class BenchmarkRun
{
    public required Guid Id { get; init; }

    public required string CaseId { get; init; }

    public required string CaseName { get; init; }

    public required Guid ModelId { get; init; }

    public string? ModelDisplayName { get; init; }

    /// <summary>Уровень рассуждений, с которым выполнялся прогон.</summary>
    public LlmReasoningEffort ReasoningEffort { get; init; }

    public ProgressStatus Status { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public DateTime? DeletedAt { get; init; }

    public string? FailureReason { get; init; }

    /// <summary>Сколько известных ошибок было в тесте.</summary>
    public int ExpectedCount { get; init; }

    /// <summary>Сколько ошибок нашла модель (включая лишние).</summary>
    public int FoundCount { get; init; }

    /// <summary>Сколько известных ошибок было найдено.</summary>
    public int MatchedCount { get; init; }

    /// <summary>Сколько известных ошибок было найдено с совпавшим заголовком.</summary>
    public int TitleMatchCount { get; init; }

    /// <summary>Сколько известных ошибок было найдено с совпавшим приоритетом.</summary>
    public int PriorityMatchCount { get; init; }

    /// <summary>Для скольких известных ошибок сравнивалось исправление.</summary>
    public int FixCheckedCount { get; init; }

    /// <summary>Для скольких известных ошибок исправление совпало.</summary>
    public int FixMatchCount { get; init; }

    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalCost { get; init; }

    public BenchmarkResult[] Results { get; init; } = [];

    /// <summary>Длительность прогона в миллисекундах, если известны обе метки времени.</summary>
    public long? DurationMs => StartedAt != null && FinishedAt != null
        ? (long)(FinishedAt.Value - StartedAt.Value).TotalMilliseconds
        : null;
}
