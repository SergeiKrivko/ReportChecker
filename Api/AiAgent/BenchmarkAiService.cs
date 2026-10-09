using AiAgent.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

/// <summary>
/// Ищет ошибки в тексте теста бенчмарка и сопоставляет их с известными,
/// используя выбранную (тестируемую) модель. Расход не записывается в статистику отчётов.
/// </summary>
public class BenchmarkAiService(
    IAiAgentFactory aiAgentFactory,
    IDifferenceService differenceService,
    IConfiguration configuration,
    ILogger<BenchmarkAiService> logger) : IBenchmarkAiService
{
    /// <summary>
    /// Группировка глав для одного запроса. В отличие от проверки отчёта,
    /// главы не отбрасываются по минимальному размеру: тест может быть коротким.
    /// </summary>
    private readonly int _maxRequestSize = int.Parse(
        configuration["Benchmarks.MaxRequestSize"] ?? configuration["Reports.MaxRequestSize"] ?? "5000");

    public async Task<BenchmarkFindResult> FindIssuesAsync(IReadOnlyList<Chapter> chapters, Guid modelId,
        LlmReasoningEffort? reasoningEffort = null, CancellationToken ct = default)
    {
        // Прогон бенчмарка не привязан к отчёту: расход не пишется в LlmUsages.
        // Значение по умолчанию совпадает с тем, что сохраняется в прогоне (None).
        await using var client = await aiAgentFactory.CreateClientAsync(modelId, LlmUsageType.Other,
            reasoningEffort ?? LlmReasoningEffort.None, null, ct);

        var differences = chapters
            .Select(e => differenceService.GetDifference(e, null))
            .ToArray();
        var issues = new List<BenchmarkFoundIssue>();
        foreach (var group in GroupChapters(differences))
        {
            var response = await client.FindIssues(new IssuesRequestAgent
            {
                Chapters = group.Select(e => e.ToAgent([], ImageProcessingMode.Disable)).ToArray(),
                Instructions = [],
            }, ct);

            foreach (var issue in response ?? [])
                issues.Add(ToFoundIssue(issue, issues.Count));
        }

        return new BenchmarkFindResult
        {
            Issues = issues.ToArray(),
            Usage = ToUsage(client.Usage),
        };
    }

    public async Task<BenchmarkMatchResult> MatchIssuesAsync(BenchmarkMatchRequest request, Guid modelId,
        CancellationToken ct = default)
    {
        await using var client = await aiAgentFactory.CreateClientAsync(modelId, LlmUsageType.Other, null, ct);

        var response = await client.MatchBenchmarkIssues(new BenchmarkMatchRequestAgent
        {
            Expected = request.Expected.Select(e => new BenchmarkExpectedAgent
            {
                Number = e.Number,
                Chapter = e.Chapter,
                Line = e.Line ?? 0,
                Title = e.Title,
                Comment = Truncate(e.Comment),
            }).ToArray(),
            Found = request.Found.Select(e => new BenchmarkFoundAgent
            {
                Index = e.Index,
                Chapter = e.Chapter ?? "",
                Line = e.Line ?? 0,
                Title = e.Title,
                Comment = Truncate(e.Comment),
            }).ToArray(),
        }, ct);

        var expectedNumbers = request.Expected.Select(e => e.Number).ToHashSet();
        var foundIndexes = request.Found.Select(e => e.Index).ToHashSet();
        var matches = (response ?? [])
            .Where(e => expectedNumbers.Contains(e.ExpectedNumber) && foundIndexes.Contains(e.FoundIndex))
            .Select(e => new BenchmarkMatch
            {
                ExpectedNumber = e.ExpectedNumber,
                FoundIndex = e.FoundIndex,
                ConfidencePercent = Math.Clamp(e.ConfidencePercent, 0, 100),
                Reason = e.Reason,
            })
            .ToArray();

        return new BenchmarkMatchResult
        {
            Matches = matches,
            Usage = ToUsage(client.Usage),
        };
    }

    private IEnumerable<ChapterDifference[]> GroupChapters(IEnumerable<ChapterDifference> differences)
    {
        var current = new List<ChapterDifference>();
        var length = 0;
        foreach (var difference in differences)
        {
            var currentLength = difference.Difference.Sum(e => e.Content.Length);
            if (length + currentLength > _maxRequestSize && current.Count > 0)
            {
                yield return current.ToArray();
                current.Clear();
                length = 0;
            }

            current.Add(difference);
            length += currentLength;
        }

        if (current.Count > 0)
            yield return current.ToArray();
    }

    private BenchmarkFoundIssue ToFoundIssue(IssueCreateAgent issue, int index)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Benchmark issue '{title}' in chapter '{chapter}'", issue.Title, issue.Chapter);
        return new BenchmarkFoundIssue
        {
            Index = index,
            Chapter = issue.Chapter,
            // Модель могла не указать строку — тогда 0 означает «не указана».
            Line = issue.Line > 0 ? issue.Line : null,
            Title = issue.Title,
            Comment = Truncate(issue.Comment),
            Priority = issue.Priority,
            Patch = issue.Patch?.Select(e => new PatchLine
            {
                Number = e.Number,
                Content = e.Content,
                Type = Enum.TryParse<PatchLineType>(e.Type, true, out var type) ? type : PatchLineType.Add,
            }).ToArray(),
        };
    }

    private static BenchmarkUsage ToUsage(AiUsageSummary usage)
    {
        return new BenchmarkUsage
        {
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            TotalTokens = usage.TotalTokens,
            TotalRequests = usage.TotalRequests,
            TotalCost = usage.TotalMoney,
        };
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Length > 4000 ? value[..4000] : value;
    }
}
