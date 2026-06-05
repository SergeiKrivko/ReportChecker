using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Exceptions;
using ReportChecker.Models.Sources;
using StackExchange.Redis;

namespace ReportChecker.Application.Services;

public class CheckService(
    ICheckRepository checkRepository,
    IReportRepository reportRepository,
    ICommentRepository commentRepository,
    IProviderService providerService,
    IServiceProvider serviceProvider,
    IAiService aiService,
    IIssueRepository issueRepository,
    ITaskCancellationService taskCancellationService,
    IConnectionMultiplexer redisConnection,
    ILogger<CheckService> logger) : ICheckService
{
    private readonly IDatabase _redis = redisConnection.GetDatabase(0);
    private readonly TimeSpan _redisTtl = TimeSpan.FromHours(1);

    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, CheckSourceUnion source, string? name = null)
    {
        var checkId = await checkRepository.CreateCheckAsync(reportId, userId, name);
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new NotFoundException($"Report with id {reportId} does not exist");

        var check = await checkRepository.GetCheckByIdAsync(checkId);
        if (check == null)
            throw new NotFoundException("Created check not found");

        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);

        if (source.Id.HasValue)
            await sourceProvider.AttachCheckAsync(source.Id.Value, checkId);
        else
            await sourceProvider.SaveAsync(checkId, source);

        var context = await GetContextAsync(report, check, true);
        _RunCheck(context);
        return checkId;
    }

    private async void _RunCheck(CheckContext context)
    {
        try
        {
            var ctSource = new CancellationTokenSource();
            taskCancellationService.AddCheckCancellationToken(context.Check.Id, ctSource);

            var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ICheckService>().RunCheckAsync(context, ctSource.Token);

            taskCancellationService.DeleteCheckCancellationToken(context.Check.Id);
        }
        catch (Exception e)
        {
            logger.LogError("Error during check processing: {e}", e);
        }
    }

    public async Task RestartLatestCheckAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new NotFoundException("Report not found");

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId, true, ct);
        if (check == null)
            throw new NotFoundException("Latest check not found");
        if (check.Status != ProgressStatus.Failed && check.Status != ProgressStatus.Cancelled)
            throw new NotFoundException($"Check with status '{check.Status}' can not be restarted");

        var context = await GetContextAsync(report, check, false, ct);
        _RunCheck(context);
    }

    public async Task RunCheckAsync(CheckContext context, CancellationToken ct = default)
    {
        var sourceProvider = providerService.GetSourceProvider(context.Report.SourceProvider);
        await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.InProgress, ct);
        await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, false);

        try
        {
            await aiService.FindIssuesAsync(context, ct);
            await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.Completed, ct);
            await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, true);
        }
        catch (Exception)
        {
            await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.Failed, ct);
            await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, true);
            throw;
        }
    }

    public async Task WriteCommentAsync(Guid reportId, Guid issueId)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new NotFoundException($"Report with id {reportId} does not exist");

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId, includeFailed: true);
        if (check == null)
            throw new NotFoundException($"Latest check of report {reportId} not found");

        var context = await GetContextAsync(report, check);
        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new NotFoundException($"Issue {issueId} not found");

        var commentId =
            await commentRepository.CreateCommentAsync(issueId, Guid.Empty, null, null, ProgressStatus.Queued);
        RunComment(context, issue, commentId);
    }

    private async void RunComment(CheckContext context, Issue issue, Guid commentId, CancellationToken ct = default)
    {
        try
        {
            var service = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IAiService>();
            await service.WriteComment(context, issue, commentId, ct);
        }
        catch (Exception e)
        {
            logger.LogError("Error during comment processing: {e}", e);
        }
    }

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(Report report, Check check)
    {
        var chapters = await GetChaptersFromCache(check.Id);
        if (chapters != null)
            return chapters;

        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var sourceStream = await sourceProvider.OpenAsync(report.Id, check.Id);

        var formatProvider = providerService.GetFormatProvider(report.Format);
        chapters = await formatProvider.GetChaptersAsync(sourceStream);
        await SaveChaptersCache(check.Id, chapters);
        return chapters;
    }

    private async Task<CheckContext> GetContextAsync(Report report, Check check, bool includePreviousCheck = false,
        CancellationToken ct = default)
    {
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var formatProvider = providerService.GetFormatProvider(report.Format);

        var chapters = await GetChaptersFromCache(check.Id);
        if (chapters == null)
        {
            var source = await sourceProvider.OpenAsync(report.Id, check.Id);
            chapters = await formatProvider.GetChaptersAsync(source);
            await SaveChaptersCache(check.Id, chapters);
        }

        var issues = await issueRepository.GetAllIssuesOfReportAsync(report.Id);

        IReadOnlyList<Chapter>? previousChapters = [];
        if (includePreviousCheck)
        {
            var previousCheck = await checkRepository.GetPreviousCheckAsync(check, ct);
            if (previousCheck != null)
            {
                previousChapters = await GetChaptersFromCache(previousCheck.Id);
                if (previousChapters == null)
                {
                    var previousSource = await sourceProvider.OpenAsync(report.Id, previousCheck.Id);
                    previousChapters = (await formatProvider.GetChaptersAsync(previousSource)).ToList();
                    await SaveChaptersCache(previousCheck.Id, previousChapters);
                }
            }
        }

        return new CheckContext
        {
            Report = report,
            Check = check,
            OldChapters = previousChapters,
            NewChapters = chapters.ToList(),
            Issues = issues.ToList(),
        };
    }

    private async Task<IReadOnlyList<Chapter>?> GetChaptersFromCache(Guid checkId)
    {
        var value = await _redis.StringGetAsync($"chapters-{checkId}");
        return value.HasValue ? JsonSerializer.Deserialize<List<Chapter>>(value.ToString()) : null;
    }

    private async Task SaveChaptersCache(Guid checkId, IEnumerable<Chapter> chapters)
    {
        await _redis.StringSetAsync($"chapters-{checkId}", JsonSerializer.Serialize(chapters), _redisTtl);
    }
}