namespace ReportChecker.Models;

/// <summary>
/// Расход токенов и стоимость одного прогона бенчмарка.
/// </summary>
public class BenchmarkUsage
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalCost { get; init; }
}

/// <summary>
/// Накопленный расход одного AI-клиента (используется для подсчёта расхода прогона).
/// </summary>
public class AiUsageSummary
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public int TotalRequests { get; init; }
    public decimal TotalMoney { get; init; }
}
