namespace ReportChecker.Models;

/// <summary>
/// Агрегированные результаты всех неудалённых прогонов по тройке (тест, модель, уровень рассуждений).
/// </summary>
public class BenchmarkSummary
{
    public required string CaseId { get; init; }

    public required Guid ModelId { get; init; }

    /// <summary>Уровень рассуждений, с которым выполнялись прогоны группы.</summary>
    public LlmReasoningEffort ReasoningEffort { get; init; }

    public string? CaseName { get; init; }

    public string? ModelDisplayName { get; init; }

    /// <summary>Всего неудалённых прогонов в группе.</summary>
    public int RunCount { get; init; }

    public int CompletedRunCount { get; init; }

    public int FailedRunCount { get; init; }

    public int CancelledRunCount { get; init; }

    /// <summary>Средняя длительность прогона в миллисекундах (по прогонам с известным временем).</summary>
    public long? AvgDurationMs { get; init; }

    public long? MinDurationMs { get; init; }

    public long? MaxDurationMs { get; init; }

    public DateTime? LastRunAt { get; init; }

    /// <summary>Сумма известных ошибок по всем прогонам группы.</summary>
    public int TotalExpected { get; init; }

    /// <summary>Сумма найденных ошибок (включая лишние) по всем прогонам группы.</summary>
    public int TotalFound { get; init; }

    /// <summary>Сумма найденных известных ошибок.</summary>
    public int TotalMatched { get; init; }

    /// <summary>Сумма «лишних» найденных ошибок.</summary>
    public int TotalExtra { get; init; }

    public int TotalTitleMatched { get; init; }
    public int TotalPriorityMatched { get; init; }
    public int TotalFixChecked { get; init; }
    public int TotalFixMatched { get; init; }

    public int TotalInputTokens { get; init; }
    public int TotalOutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalCost { get; init; }

    /// <summary>Доля найденных известных ошибок (recall).</summary>
    public double MatchedShare => TotalExpected == 0 ? 0 : (double)TotalMatched / TotalExpected;

    /// <summary>Доля найденных, у которых совпал заголовок (precision заголовков).</summary>
    public double TitleMatchShare => TotalMatched == 0 ? 0 : (double)TotalTitleMatched / TotalMatched;

    public double PriorityMatchShare => TotalMatched == 0 ? 0 : (double)TotalPriorityMatched / TotalMatched;

    public double FixMatchShare => TotalFixChecked == 0 ? 0 : (double)TotalFixMatched / TotalFixChecked;
}
