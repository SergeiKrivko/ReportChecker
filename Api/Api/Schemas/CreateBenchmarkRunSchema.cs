using ReportChecker.Models;

namespace ReportChecker.Api.Schemas;

public class CreateBenchmarkRunSchema
{
    /// <summary>Идентификаторы тестов; пусто или не задано — все доступные тесты.</summary>
    public string[] CaseIds { get; init; } = [];

    public required Guid[] ModelIds { get; init; }

    /// <summary>Разрешить LLM-досопоставление остатка (по умолчанию — конфигурация).</summary>
    public bool? UseLlmMatching { get; init; }

    /// <summary>Уровень рассуждений тестируемой модели; пусто — значение по умолчанию фабрики.</summary>
    public LlmReasoningEffort? ReasoningEffort { get; init; }
}
