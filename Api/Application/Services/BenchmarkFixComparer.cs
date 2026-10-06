using ReportChecker.Models;

namespace ReportChecker.Application.Services;

/// <summary>
/// Сравнение предложенного моделью исправления с эталонным.
/// Патч применяется к тексту главы тем же алгоритмом, что и при реальном применении (Add/Delete/Modify).
/// </summary>
public class BenchmarkFixComparer
{
    /// <summary>
    /// Применяет патч к тексту главы и возвращает новый текст.
    /// Семантика совпадает с <c>LatexFormatProvider.ApplyPatchAsync</c>:
    /// <c>Add</c> добавляет строку после указанной (0 — в начало),
    /// <c>Modify</c> заменяет содержимое строки, <c>Delete</c> удаляет строку.
    /// </summary>
    public string ApplyPatch(string chapterContent, IReadOnlyCollection<PatchLine> lines)
    {
        var source = SplitLines(chapterContent);
        var result = new List<string>();

        for (var i = 0; i < source.Count; i++)
        {
            var lineNumber = i + 1;
            var current = lines.Where(e => e.Number == lineNumber).ToList();
            var deleted = current.Any(e => e.Type == PatchLineType.Delete);
            if (!deleted)
            {
                var modify = current.FirstOrDefault(e => e.Type == PatchLineType.Modify);
                result.Add(modify != null ? modify.Content ?? "" : source[i]);
            }

            foreach (var add in current.Where(e => e.Type == PatchLineType.Add))
            {
                result.Add(add.Content ?? "");
            }
        }

        return string.Join('\n', result);
    }

    /// <summary>
    /// Сравнивает эталонное и найденное исправления, применённые к тексту главы.
    /// Регистр учитывается: он важен для орфографии.
    /// </summary>
    public BenchmarkFixMatchStatus Compare(BenchmarkExpectedIssue expected, string? chapterContent,
        BenchmarkFoundIssue? found)
    {
        if (!expected.CheckFix)
            return BenchmarkFixMatchStatus.NotApplicable;
        // Сравнение запрошено, но эталонное исправление в тесте не задано.
        if (expected.Patch is not { Length: > 0 })
            return BenchmarkFixMatchStatus.MissingExpectedPatch;
        if (found == null)
            return BenchmarkFixMatchStatus.MissingFoundPatch;
        if (found.Patch is not { Length: > 0 })
            return BenchmarkFixMatchStatus.MissingFoundPatch;

        var expectedText = ApplyPatch(chapterContent ?? "", expected.Patch);
        var foundText = ApplyPatch(chapterContent ?? "", found.Patch);
        return AreEquivalent(expectedText, foundText)
            ? BenchmarkFixMatchStatus.Matched
            : BenchmarkFixMatchStatus.Mismatched;
    }

    /// <summary>Применённый текст эталонного исправления (для сохранения в БД).</summary>
    public string AppliedExpectedFix(BenchmarkExpectedIssue expected, string? chapterContent)
    {
        return expected.Patch is { Length: > 0 } ? ApplyPatch(chapterContent ?? "", expected.Patch) : "";
    }

    /// <summary>Применённый текст найденного исправления (для сохранения в БД).</summary>
    public string AppliedFoundFix(BenchmarkFoundIssue? found, string? chapterContent)
    {
        return found?.Patch is { Length: > 0 } ? ApplyPatch(chapterContent ?? "", found.Patch) : "";
    }

    /// <summary>Сравнивает тексты с нормализацией переводов строк, концевых пробелов и хвостовых пустых строк.</summary>
    public bool AreEquivalent(string? expected, string? actual)
    {
        return NormalizeText(expected) == NormalizeText(actual);
    }

    internal static List<string> SplitLines(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').ToList();
    }

    private static string NormalizeText(string? value)
    {
        if (value == null)
            return "";
        var lines = SplitLines(value)
            .Select(e => e.TrimEnd())
            .ToList();
        while (lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);
        return string.Join('\n', lines);
    }
}
