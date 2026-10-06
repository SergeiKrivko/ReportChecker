namespace ReportChecker.Models;

/// <summary>
/// Режим показа теста на фронте.
/// </summary>
public enum BenchmarkCaseDisplayMode
{
    /// <summary>Показывать всегда.</summary>
    Always = 0,

    /// <summary>Показывать свёрнутым.</summary>
    Collapsed = 1,

    /// <summary>Не показывать вообще.</summary>
    Hidden = 2,
}
