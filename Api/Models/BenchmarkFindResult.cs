namespace ReportChecker.Models;

/// <summary>
/// Результат поиска ошибок моделью в тексте теста бенчмарка.
/// </summary>
public class BenchmarkFindResult
{
    public BenchmarkFoundIssue[] Issues { get; init; } = [];

    public BenchmarkUsage Usage { get; init; } = new();
}

/// <summary>
/// Запрос на создание прогонов бенчмарка.
/// </summary>
public class BenchmarkRunRequest
{
    /// <summary>Идентификаторы тестов; пусто — все доступные тесты.</summary>
    public string[] CaseIds { get; init; } = [];

    public Guid[] ModelIds { get; init; } = [];

    /// <summary>Разрешить LLM-досопоставление остатка (иначе только детерминированное).</summary>
    public bool? UseLlmMatching { get; init; }
}
