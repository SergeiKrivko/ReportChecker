using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

/// <summary>
/// Запускает прогоны бенчмарка и собирает их результаты.
/// </summary>
public class BenchmarkService(
    IBenchmarkRepository benchmarkRepository,
    IBenchmarkCaseProvider caseProvider,
    IBenchmarkAiService benchmarkAiService,
    BenchmarkRunLimiter runLimiter,
    IServiceProvider serviceProvider,
    ITaskCancellationService taskCancellationService,
    IConfiguration configuration,
    ILogger<BenchmarkService> logger) : IBenchmarkService
{
    private readonly BenchmarkMatcher _matcher = new();
    private readonly BenchmarkFixComparer _fixComparer = new();

    private readonly int _lineTolerance = int.Parse(configuration["Benchmarks.LineTolerance"] ?? "3");

    private readonly Guid _judgeModelId = Guid.Parse(configuration["Benchmarks.JudgeModelId"] ??
                                                     configuration["Ai.DefaultModelId"] ??
                                                     throw new Exception("Model id not set"));

    private readonly double _titleSimilarityThreshold = double.Parse(
        configuration["Benchmarks.TitleSimilarityThreshold"] ?? "0.6", CultureInfo.InvariantCulture);

    private readonly bool _llmMatching = bool.Parse(configuration["Benchmarks.LlmMatching"] ?? "true");

    private readonly int _llmConfidenceThreshold =
        int.Parse(configuration["Benchmarks.LlmConfidenceThreshold"] ?? "60");

    private BenchmarkMatchOptions DefaultOptions => new()
    {
        LineTolerance = _lineTolerance,
        TitleSimilarityThreshold = _titleSimilarityThreshold,
        LlmMatching = _llmMatching,
        LlmConfidenceThreshold = _llmConfidenceThreshold,
    };

    public async Task<IReadOnlyList<Guid>> CreateRunsAsync(BenchmarkRunRequest request,
        CancellationToken ct = default)
    {
        if (request.ModelIds.Length == 0)
            throw new BadRequestException("Не задано ни одной модели");

        var allCases = caseProvider.GetCases();
        if (allCases.Count == 0)
            throw new NotFoundException("Не найдено ни одного теста бенчмарка");

        var cases = request.CaseIds.Length == 0
            ? allCases.Where(e => e.ValidationError == null).ToArray()
            : request.CaseIds.Select(caseId => allCases.FirstOrDefault(e => e.Id == caseId)
                                               ?? throw new NotFoundException($"Тест '{caseId}' не найден")).ToArray();

        foreach (var benchmarkCase in cases.Where(e => e.ValidationError != null))
            throw new BadRequestException($"Тест '{benchmarkCase.Id}' некорректен: {benchmarkCase.ValidationError}");

        if (cases.Length == 0)
            throw new NotFoundException("Не найдено ни одного корректного теста бенчмарка");

        var runIds = new List<Guid>();
        foreach (var benchmarkCase in cases)
        foreach (var modelId in request.ModelIds.Distinct())
        {
            var runId = await benchmarkRepository.CreateRunAsync(benchmarkCase.Id, benchmarkCase.Name, modelId, ct);
            runIds.Add(runId);
            RunInBackground(runId, benchmarkCase.Id, modelId, request.UseLlmMatching);
        }

        return runIds;
    }

    private void RunInBackground(Guid runId, string caseId, Guid modelId, bool? useLlmMatching)
    {
        var cancellation = new CancellationTokenSource();
        taskCancellationService.AddBenchmarkCancellationToken(runId, cancellation);
        _ = Task.Run(async () =>
        {
            await runLimiter.WaitAsync(CancellationToken.None);
            try
            {
                using var scope = serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBenchmarkService>();
                await service.RunAsync(runId, caseId, modelId, useLlmMatching, cancellation.Token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Непредвиденная ошибка при выполнении прогона бенчмарка '{runId}'", runId);
                // Репозиторий исходного запроса к этому моменту уже может быть удалён,
                // поэтому обращаемся к БД через собственный scope.
                using var scope = serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IBenchmarkRepository>();
                await repository.FailRunAsync(runId, e.Message, DateTime.UtcNow);
            }
            finally
            {
                runLimiter.Release();
                taskCancellationService.DeleteBenchmarkCancellationToken(runId);
            }
        }, CancellationToken.None);
    }

    public async Task RunAsync(Guid runId, string caseId, Guid modelId, bool? useLlmMatching,
        CancellationToken ct = default)
    {
        await benchmarkRepository.SetStatusAsync(runId, ProgressStatus.InProgress, ct);
        try
        {
            var benchmarkCase = caseProvider.GetCase(caseId)
                                ?? throw new NotFoundException($"Тест '{caseId}' не найден");
            var chapters = await caseProvider.GetChaptersAsync(benchmarkCase, ct);
            var chapterContents = chapters.ToDictionary(e => e.Name, e => e.Content);

            var findResult = await benchmarkAiService.FindIssuesAsync(chapters, modelId, ct);
            var options = DefaultOptions with { LlmMatching = useLlmMatching ?? _llmMatching };

            var outcome = _matcher.Match(benchmarkCase.Expected, findResult.Issues, options);
            var usage = findResult.Usage;
            if (outcome.Pending != null)
            {
                var matchResult = await benchmarkAiService.MatchIssuesAsync(outcome.Pending, _judgeModelId, ct);
                outcome = _matcher.ApplyLlmMatches(outcome, matchResult.Matches, options);
                // usage = Sum(usage, matchResult.Usage);
            }

            var (results, aggregates) = BuildResults(runId, benchmarkCase, findResult.Issues, outcome,
                chapterContents);
            await benchmarkRepository.CompleteRunAsync(runId, results, aggregates, usage, DateTime.UtcNow, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Прогон бенчмарка '{runId}' отменён", runId);
            await benchmarkRepository.SetStatusAsync(runId, ProgressStatus.Cancelled);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Прогон бенчмарка '{runId}' завершился ошибкой", runId);
            await benchmarkRepository.FailRunAsync(runId, e.ToString(), DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Создаёт по строке на каждую известную ошибку (найдена/не найдена) и на каждую «лишнюю».
    /// </summary>
    private (BenchmarkResult[] Results, BenchmarkRun Aggregates) BuildResults(Guid runId,
        BenchmarkCase benchmarkCase, IReadOnlyCollection<BenchmarkFoundIssue> found,
        BenchmarkMatchOutcome outcome, IReadOnlyDictionary<string, string> chapterContents)
    {
        var results = new List<BenchmarkResult>();
        var createdAt = DateTime.UtcNow;
        var pairsByExpected = outcome.Pairs.ToDictionary(e => e.ExpectedNumber);
        var foundByIndex = found.ToDictionary(e => e.Index);

        var titleMatches = 0;
        var priorityMatches = 0;
        var fixChecked = 0;
        var fixMatched = 0;

        foreach (var expected in benchmarkCase.Expected.OrderBy(e => e.Number))
        {
            var chapterContent = chapterContents.GetValueOrDefault(expected.Chapter);
            var pair = pairsByExpected.GetValueOrDefault(expected.Number);
            var foundIssue = pair == null ? null : foundByIndex.GetValueOrDefault(pair.FoundIndex);

            var titleMatch = foundIssue == null
                ? (bool?)null
                : _matcher.Similarity(expected.Title, foundIssue.Title) >= _titleSimilarityThreshold;
            var priorityMatch = foundIssue == null
                ? (bool?)null
                : expected.Priority == foundIssue.Priority;
            if (titleMatch == true)
                titleMatches++;
            if (priorityMatch == true)
                priorityMatches++;

            var fixStatus = _fixComparer.Compare(expected, chapterContent, foundIssue);
            if (fixStatus != BenchmarkFixMatchStatus.NotApplicable)
                fixChecked++;
            if (fixStatus == BenchmarkFixMatchStatus.Matched)
                fixMatched++;

            results.Add(new BenchmarkResult
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                CreatedAt = createdAt,
                ExpectedNumber = expected.Number,
                ErrorClass = expected.ErrorClass,
                Chapter = expected.Chapter,
                Line = foundIssue?.Line ?? expected.Line,
                ExpectedTitle = expected.Title,
                ExpectedComment = expected.Comment,
                ExpectedPriority = expected.Priority,
                FoundIndex = foundIssue?.Index,
                FoundTitle = foundIssue?.Title,
                FoundComment = foundIssue?.Comment,
                FoundPriority = foundIssue?.Priority,
                IsFound = foundIssue != null,
                TitleMatch = titleMatch,
                PriorityMatch = priorityMatch,
                PriorityDelta = foundIssue == null ? null : foundIssue.Priority - expected.Priority,
                MatchingMethod = pair?.Method ?? BenchmarkMatchMethod.None,
                MatchScore = pair?.Score,
                MatchingReason = pair?.Reason
                                 ?? (foundIssue == null ? "Известная ошибка не найдена" : null),
                FixMatchStatus = fixStatus,
                ExpectedFix = Trim(_fixComparer.AppliedExpectedFix(expected, chapterContent)),
                FoundFix = Trim(_fixComparer.AppliedFoundFix(foundIssue, chapterContent)),
            });
        }

        foreach (var extraIssue in outcome.ExtraFound)
        {
            var chapterContent = chapterContents.GetValueOrDefault(extraIssue.Chapter);
            results.Add(new BenchmarkResult
            {
                Id = Guid.NewGuid(),
                RunId = runId,
                CreatedAt = createdAt,
                ExpectedNumber = null,
                Chapter = extraIssue.Chapter,
                Line = extraIssue.Line,
                FoundIndex = extraIssue.Index,
                FoundTitle = extraIssue.Title,
                FoundComment = extraIssue.Comment,
                FoundPriority = extraIssue.Priority,
                IsFound = true,
                MatchingMethod = BenchmarkMatchMethod.None,
                MatchingReason = "Ошибка не соответствует ни одной известной",
                FixMatchStatus = BenchmarkFixMatchStatus.NotApplicable,
                FoundFix = Trim(_fixComparer.AppliedFoundFix(extraIssue, chapterContent)),
            });
        }

        var aggregates = new BenchmarkRun
        {
            Id = runId,
            CaseId = benchmarkCase.Id,
            CaseName = benchmarkCase.Name,
            ModelId = Guid.Empty,
            ExpectedCount = benchmarkCase.Expected.Length,
            FoundCount = found.Count,
            MatchedCount = outcome.Pairs.Length,
            TitleMatchCount = titleMatches,
            PriorityMatchCount = priorityMatches,
            FixCheckedCount = fixChecked,
            FixMatchCount = fixMatched,
        };
        return (results.ToArray(), aggregates);
    }

    private static BenchmarkUsage Sum(BenchmarkUsage left, BenchmarkUsage right)
    {
        return new BenchmarkUsage
        {
            InputTokens = left.InputTokens + right.InputTokens,
            OutputTokens = left.OutputTokens + right.OutputTokens,
            TotalTokens = left.TotalTokens + right.TotalTokens,
            TotalRequests = left.TotalRequests + right.TotalRequests,
            TotalCost = left.TotalCost + right.TotalCost,
        };
    }

    private static string? Trim(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        return value.Length > 20000 ? value[..20000] : value;
    }

    public Task<IReadOnlyList<BenchmarkRun>> GetRunsAsync(string? caseId = null, Guid? modelId = null,
        ProgressStatus? status = null, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        return benchmarkRepository.GetRunsAsync(caseId, modelId, status, limit, offset, ct);
    }

    public Task<BenchmarkRun?> GetRunAsync(Guid runId, CancellationToken ct = default)
    {
        return benchmarkRepository.GetRunByIdAsync(runId, false, ct);
    }

    public Task<IReadOnlyList<BenchmarkSummary>> GetSummaryAsync(string? caseId = null, Guid? modelId = null,
        CancellationToken ct = default)
    {
        return benchmarkRepository.GetSummaryAsync(caseId, modelId, ct);
    }

    public async Task<bool> DeleteRunAsync(Guid runId, CancellationToken ct = default)
    {
        var run = await benchmarkRepository.GetRunByIdAsync(runId, false, ct);
        if (run == null)
            return false;

        if (run.Status is ProgressStatus.Queued or ProgressStatus.InProgress)
            await taskCancellationService.CancelBenchmarkAsync(runId);

        return await benchmarkRepository.SoftDeleteAsync(runId, ct);
    }
}