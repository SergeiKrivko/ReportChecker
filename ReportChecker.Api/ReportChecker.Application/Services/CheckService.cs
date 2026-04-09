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
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);

        if (source.Id.HasValue)
            await sourceProvider.AttachCheckAsync(source.Id.Value, checkId);
        else
            await sourceProvider.SaveAsync(checkId, source);

        var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICheckService>().RunCheck(checkId);
        return checkId;
    }

    public async void RunCheck(Guid checkId)
    {
        try
        {
            var check = await checkRepository.GetCheckByIdAsync(checkId);
            if (check == null)
                throw new ArgumentException($"Check with id {checkId} does not exist");
            var report = await reportRepository.GetReportByIdAsync(check.ReportId);
            if (report == null)
                throw new ArgumentException($"Report with id {check.ReportId} does not exist");
            var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
            var sourceStream = await sourceProvider.OpenAsync(report.Id, check.Id);
            await RunCheck(report, check, sourceStream);
        }
        catch (Exception e)
        {
            logger.LogError("Error during check processing: {e}", e);
        }
    }

    public async void RunCheck(Guid checkId, IFileArchive source)
    {
        try
        {
            var check = await checkRepository.GetCheckByIdAsync(checkId);
            if (check == null)
                throw new ArgumentException($"Check with id {checkId} does not exist");
            var report = await reportRepository.GetReportByIdAsync(check.ReportId);
            if (report == null)
                throw new ArgumentException($"Report with id {check.ReportId} does not exist");
            await RunCheck(report, check, source);
        }
        catch (Exception e)
        {
            logger.LogError("Error during check processing: {e}", e);
        }
    }

    private async Task RunCheck(Report report, Check check, IFileArchive source)
    {
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.InProgress);
        await sourceProvider.WriteCheckStatusAsync(report, check, false);

        try
        {
            var formatProvider = providerService.GetFormatProvider(report.Format);
            var chapters = await formatProvider.GetChaptersAsync(source);
            var issues = await issueRepository.GetAllIssuesOfReportAsync(report.Id);

            List<Chapter> previousChapters = [];
            var previousCheck = await checkRepository.GetPreviousCheckAsync(check);
            if (previousCheck != null)
            {
                var previousSource =
                    await sourceProvider.OpenAsync(report.Id, previousCheck.Id);
                previousChapters = (await formatProvider.GetChaptersAsync(previousSource)).ToList();
            }

            await aiService.FindIssuesAsync(report.Id, check.Id, chapters.ToList(), previousChapters, issues.ToList());
            await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.Completed);
            await sourceProvider.WriteCheckStatusAsync(report, check, true);
        }
        catch (Exception)
        {
            await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.Failed);
            await sourceProvider.WriteCheckStatusAsync(report, check, true);
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

        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var sourceStream = await sourceProvider.OpenAsync(report.Id, check.Id);

        var formatProvider = providerService.GetFormatProvider(report.Format);
        var chapters = await formatProvider.GetChaptersAsync(sourceStream);
        RunComment(report, issueId, chapters.ToList());
    }

    private async void RunComment(Report report, Guid issueId, List<Chapter> chapters)
    {
        try
        {
            var service = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IAiService>();
            await service.WriteComment(report, issueId, chapters);
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
}