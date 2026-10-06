namespace ReportChecker.Models;

/// <summary>
/// Способ, которым найденная ошибка была сопоставлена с известной.
/// </summary>
public enum BenchmarkMatchMethod
{
    /// <summary>Сопоставление не выполнялось (ошибка не найдена или найдена лишняя).</summary>
    None = 0,

    /// <summary>Детерминированное сопоставление по главе, строке и похожести заголовка.</summary>
    Deterministic = 1,

    /// <summary>Сопоставление моделью (LLM) для неоднозначных случаев.</summary>
    Llm = 2,
}
