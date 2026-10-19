namespace ReportChecker.Models;

/// <summary>
/// Текст теста бенчмарка с известными ошибками, разбитый на главы и строки.
/// Строки пронумерованы так же, как их видит модель: номер внутри главы с единицы,
/// поэтому он совпадает с полем <c>line</c> в <c>case.json</c>.
/// </summary>
public class BenchmarkCaseText
{
    public required string CaseId { get; init; }

    public required string CaseName { get; init; }

    /// <summary>Ключ провайдера формата (например, <c>Latex</c>).</summary>
    public required string Format { get; init; }

    public BenchmarkCaseChapter[] Chapters { get; init; } = [];
}
