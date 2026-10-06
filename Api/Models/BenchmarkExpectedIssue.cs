namespace ReportChecker.Models;

/// <summary>
/// Известная (эталонная) ошибка теста бенчмарка.
/// </summary>
public class BenchmarkExpectedIssue
{
    /// <summary>Номер известной ошибки, уникальный внутри теста.</summary>
    public required int Number { get; init; }

    public required string Chapter { get; init; }

    public int? Line { get; init; }

    public required string Title { get; init; }

    public required string Comment { get; init; }

    public int Priority { get; init; } = 1;

    /// <summary>Класс ошибки (используется для группировки результатов).</summary>
    public string? ErrorClass { get; init; }

    /// <summary>Нужно ли сравнивать предложенное исправление с эталонным.</summary>
    public bool CheckFix { get; init; }

    /// <summary>Эталонное исправление в том же формате, что возвращает агент.</summary>
    public PatchLine[]? Patch { get; init; }
}
