using System.Text.RegularExpressions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

/// <summary>
/// Настройки сопоставления найденных ошибок с известными.
/// </summary>
public record BenchmarkMatchOptions
{
    public int LineTolerance { get; init; } = 3;
    public double TitleSimilarityThreshold { get; init; } = 0.6;
    public bool LlmMatching { get; init; } = true;
    public int LlmConfidenceThreshold { get; init; } = 60;
}

/// <summary>
/// Сопоставление известной ошибки с найденной.
/// </summary>
public class BenchmarkMatchPair
{
    public required int ExpectedNumber { get; init; }
    public required int FoundIndex { get; init; }
    public BenchmarkMatchMethod Method { get; init; }
    public double Score { get; init; }
    public double TitleSimilarity { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Итог сопоставления: пары, ненайденные известные ошибки, «лишние» найденные ошибки
/// и остаток, который можно отдать модели.
/// </summary>
public class BenchmarkMatchOutcome
{
    public BenchmarkMatchPair[] Pairs { get; init; } = [];
    public BenchmarkExpectedIssue[] NotFoundExpected { get; init; } = [];
    public BenchmarkFoundIssue[] ExtraFound { get; init; } = [];

    /// <summary>Остаток для LLM-досопоставления; <c>null</c> если сопоставлять нечего.</summary>
    public BenchmarkMatchRequest? Pending { get; init; }
}

/// <summary>
/// Гибридное сопоставление: сначала детерминированно (глава, строка, похожесть заголовка),
/// затем — остаток одним запросом к модели.
/// </summary>
public class BenchmarkMatcher
{
    private static readonly Regex HtmlTagRegex = new("<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex NonWordRegex = new("[^\\p{L}\\p{Nd}]+", RegexOptions.Compiled);

    /// <summary>Детерминированное сопоставление. Остаток возвращается в <see cref="BenchmarkMatchOutcome.Pending"/>.</summary>
    public BenchmarkMatchOutcome Match(IReadOnlyCollection<BenchmarkExpectedIssue> expected,
        IReadOnlyCollection<BenchmarkFoundIssue> found, BenchmarkMatchOptions options)
    {
        var candidates = new List<(int ExpectedNumber, int FoundIndex, double Score, double Similarity, string Reason)>();
        foreach (var expectedIssue in expected)
        {
            foreach (var foundIssue in found)
            {
                if (!ChapterMatches(expectedIssue.Chapter, foundIssue.Chapter))
                    continue;

                var similarity = Similarity(expectedIssue.Title, foundIssue.Title);
                var linesEqual = expectedIssue.Line != null && foundIssue.Line != null &&
                                 expectedIssue.Line == foundIssue.Line;
                var linesClose = expectedIssue.Line != null && foundIssue.Line != null &&
                                 Math.Abs(expectedIssue.Line.Value - foundIssue.Line.Value) <= options.LineTolerance;
                if (!linesEqual &&
                    !(linesClose && similarity >= options.TitleSimilarityThreshold) &&
                    similarity < options.TitleSimilarityThreshold)
                    continue;

                var bonus = linesEqual ? 0.5 : linesClose ? 0.25 : 0;
                var reason = linesEqual
                    ? "Совпали глава и строка"
                    : $"Похожесть заголовков {similarity:P0}";
                candidates.Add((expectedIssue.Number, foundIssue.Index, similarity + bonus, similarity, reason));
            }
        }

        var pairs = new List<BenchmarkMatchPair>();
        var usedExpected = new HashSet<int>();
        var usedFound = new HashSet<int>();
        foreach (var candidate in candidates
                     .OrderByDescending(e => e.Score)
                     .ThenBy(e => e.ExpectedNumber)
                     .ThenBy(e => e.FoundIndex))
        {
            // Сначала проверяем обе стороны пары, только потом помечаем их использованными:
            // иначе отклонённая пара «сожгла» бы свободную известную ошибку.
            if (usedExpected.Contains(candidate.ExpectedNumber) || usedFound.Contains(candidate.FoundIndex))
                continue;
            usedExpected.Add(candidate.ExpectedNumber);
            usedFound.Add(candidate.FoundIndex);
            pairs.Add(new BenchmarkMatchPair
            {
                ExpectedNumber = candidate.ExpectedNumber,
                FoundIndex = candidate.FoundIndex,
                Method = BenchmarkMatchMethod.Deterministic,
                Score = candidate.Score,
                TitleSimilarity = candidate.Similarity,
                Reason = candidate.Reason,
            });
        }

        var notFound = expected.Where(e => !usedExpected.Contains(e.Number)).ToArray();
        var extra = found.Where(e => !usedFound.Contains(e.Index)).ToArray();
        var pending = options.LlmMatching && notFound.Length > 0 && extra.Length > 0
            ? new BenchmarkMatchRequest { Expected = notFound, Found = extra }
            : null;

        return new BenchmarkMatchOutcome
        {
            Pairs = pairs.ToArray(),
            NotFoundExpected = notFound,
            ExtraFound = extra,
            Pending = pending,
        };
    }

    /// <summary>
    /// Дополняет результат детерминированного сопоставления парами, предложенными моделью,
    /// и возвращает итог с пересчитанными ненайденными и «лишними» ошибками.
    /// Принимаются пары с уверенностью не ниже порога, только для ещё не сопоставленных ошибок.
    /// </summary>
    public BenchmarkMatchOutcome ApplyLlmMatches(BenchmarkMatchOutcome outcome,
        IReadOnlyCollection<BenchmarkMatch> matches, BenchmarkMatchOptions options)
    {
        var pairs = outcome.Pairs.ToList();
        var usedExpected = pairs.Select(e => e.ExpectedNumber).ToHashSet();
        var usedFound = pairs.Select(e => e.FoundIndex).ToHashSet();
        foreach (var match in matches
                     .Where(e => e.ConfidencePercent >= options.LlmConfidenceThreshold)
                     .OrderByDescending(e => e.ConfidencePercent))
        {
            if (outcome.NotFoundExpected.All(e => e.Number != match.ExpectedNumber))
                continue;
            if (outcome.ExtraFound.All(e => e.Index != match.FoundIndex))
                continue;
            if (usedExpected.Contains(match.ExpectedNumber) || usedFound.Contains(match.FoundIndex))
                continue;
            usedExpected.Add(match.ExpectedNumber);
            usedFound.Add(match.FoundIndex);
            pairs.Add(new BenchmarkMatchPair
            {
                ExpectedNumber = match.ExpectedNumber,
                FoundIndex = match.FoundIndex,
                Method = BenchmarkMatchMethod.Llm,
                Score = match.ConfidencePercent / 100.0,
                TitleSimilarity = Similarity(
                    outcome.NotFoundExpected.First(e => e.Number == match.ExpectedNumber).Title,
                    outcome.ExtraFound.First(e => e.Index == match.FoundIndex).Title),
                Reason = match.Reason ?? $"Сопоставлено моделью ({match.ConfidencePercent}%)",
            });
        }

        return new BenchmarkMatchOutcome
        {
            Pairs = pairs.ToArray(),
            NotFoundExpected = outcome.NotFoundExpected.Where(e => !usedExpected.Contains(e.Number)).ToArray(),
            ExtraFound = outcome.ExtraFound.Where(e => !usedFound.Contains(e.Index)).ToArray(),
            Pending = null,
        };
    }

    /// <summary>Похожесть заголовков: максимум из Jaccard по токенам и нормализованного расстояния Левенштейна.</summary>
    public double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);
        if (a.Length == 0 && b.Length == 0)
            return 1;
        if (a.Length == 0 || b.Length == 0)
            return 0;
        if (a == b)
            return 1;

        var tokensA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var tokensB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var union = tokensA.Union(tokensB).Count();
        var jaccard = union == 0 ? 0 : (double)tokensA.Intersect(tokensB).Count() / union;

        var distance = Levenshtein(a, b);
        var ratio = 1 - (double)distance / Math.Max(a.Length, b.Length);
        return Math.Max(jaccard, ratio);
    }

    private static bool ChapterMatches(string? left, string? right)
    {
        return string.Equals((left ?? "").Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        var withoutTags = HtmlTagRegex.Replace(value, " ");
        var normalized = NonWordRegex.Replace(withoutTags.ToLowerInvariant(), " ");
        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int Levenshtein(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
