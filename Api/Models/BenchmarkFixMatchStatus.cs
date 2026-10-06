namespace ReportChecker.Models;

/// <summary>
/// Результат сравнения предложенного исправления с эталонным.
/// </summary>
public enum BenchmarkFixMatchStatus
{
    /// <summary>Сравнение не требуется (класс ошибки без проверки исправления).</summary>
    NotApplicable = 0,

    /// <summary>Исправление совпало с эталонным.</summary>
    Matched = 1,

    /// <summary>Исправление было, но отличается от эталонного.</summary>
    Mismatched = 2,

    /// <summary>Модель предложила исправление, но эталонное не задано.</summary>
    MissingExpectedPatch = 3,

    /// <summary>Эталонное исправление ожидалось, но модель его не предложила.</summary>
    MissingFoundPatch = 4,
}
