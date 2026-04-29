using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Application.Services;

public class CheckService(
    ICheckRepository checkRepository,
    IReportRepository reportRepository,
    IProviderService providerService,
    IServiceProvider serviceProvider,
    IAiService aiService,
    IIssueRepository issueRepository,
    ILogger<CheckService> logger) : ICheckService
{
    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, CheckSourceUnion source, string? name = null)
    {
        var checkId = await checkRepository.CreateCheckAsync(reportId, userId, name);
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new ArgumentException($"Report with id {reportId} does not exist");

        var check = await checkRepository.GetCheckByIdAsync(checkId);
        if (check == null)
            throw new Exception("Created check not found");

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
            var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ICheckService>().RunCheck(context);
        }
        catch (Exception e)
        {
            logger.LogError("Error during check processing: {e}", e);
        }
    }

    public async Task RunCheck(CheckContext context)
    {
        var sourceProvider = providerService.GetSourceProvider(context.Report.SourceProvider);
        await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.InProgress);
        await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, false);

        try
        {
            await aiService.FindIssuesAsync(context);
            await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.Completed);
            await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, true);
        }
        catch (Exception)
        {
            await checkRepository.SetCheckStatusAsync(context.Check.Id, ProgressStatus.Failed);
            await sourceProvider.WriteCheckStatusAsync(context.Report, context.Check, true);
            throw;
        }
    }

    public async Task WriteCommentAsync(Guid reportId, Guid issueId)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new ArgumentException($"Report with id {reportId} does not exist");

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId);
        if (check == null)
            throw new ArgumentException($"Latest check of report {reportId} not found");

        var context = await GetContextAsync(report, check, false);
        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new ArgumentException($"Issue {issueId} not found");

        RunComment(context, issue);
    }

    private async void RunComment(CheckContext context, Issue issue)
    {
        try
        {
            var service = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IAiService>();
            await service.WriteComment(context, issue);
        }
        catch (Exception e)
        {
            logger.LogError("Error during comment processing: {e}", e);
        }
    }

    public async Task<IEnumerable<Chapter>> GetChaptersAsync(Report report, Check check)
    {
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var sourceStream = await sourceProvider.OpenAsync(report.Id, check.Id);

        var formatProvider = providerService.GetFormatProvider(report.Format);
        var chapters = await formatProvider.GetChaptersAsync(sourceStream);
        return chapters;
    }

    private async Task<CheckContext> GetContextAsync(Report report, Check check, bool includePreviousCheck = false)
    {
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var formatProvider = providerService.GetFormatProvider(report.Format);

        var source = await sourceProvider.OpenAsync(report.Id, check.Id);
        var chapters = await formatProvider.GetChaptersAsync(source);
        var issues = await issueRepository.GetAllIssuesOfReportAsync(report.Id);

        List<Chapter> previousChapters = [];
        if (includePreviousCheck)
        {
            var previousCheck = await checkRepository.GetPreviousCheckAsync(check);
            if (previousCheck != null)
            {
                var previousSource = await sourceProvider.OpenAsync(report.Id, previousCheck.Id);
                previousChapters = (await formatProvider.GetChaptersAsync(previousSource)).ToList();
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
}