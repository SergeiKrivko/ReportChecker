using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

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
    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, string source, string? name = null)
    {
        var checkId = await checkRepository.CreateCheckAsync(reportId, userId, source, name);
        var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICheckService>().RunCheck(checkId);
        return checkId;
    }

    public async Task<Guid> CreateCheckAsync(Guid reportId, Guid userId, SourceSchema source)
    {
        var checkId = await checkRepository.CreateCheckAsync(reportId, userId, source.Source, source.Name);
        var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICheckService>().RunCheck(checkId, source.Stream);
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
            var sourceStream =
                await sourceProvider.GetStreamAsync(check.Source ?? throw new Exception("Source is null"));
            await RunCheck(report, check, sourceStream);
        }
        catch (Exception e)
        {
            logger.LogError("Error during check processing: {e}", e);
        }
    }

    public async void RunCheck(Guid checkId, Stream source)
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

    private async Task RunCheck(Report report, Check check, Stream source)
    {
        await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.InProgress);

        try
        {
            var formatProvider = providerService.GetFormatProvider(report.Format);
            var chapters = await formatProvider.GetChaptersAsync(source);
            var issues = await issueRepository.GetAllIssuesOfReportAsync(report.Id);
            await aiService.FindIssuesAsync(check.Id, chapters, issues.ToList());
            await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.Completed);
        }
        catch (Exception)
        {
            await checkRepository.SetCheckStatusAsync(check.Id, ProgressStatus.Failed);
            throw;
        }
    }

    public async Task WriteCommentAsync(Guid checkId, Guid issueId)
    {
        var check = await checkRepository.GetCheckByIdAsync(checkId);
        if (check == null)
            throw new ArgumentException($"Check with id {checkId} does not exist");

        var report = await reportRepository.GetReportByIdAsync(check.ReportId);
        if (report == null)
            throw new ArgumentException($"Report with id {check.ReportId} does not exist");

        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var sourceStream =
            await sourceProvider.GetStreamAsync(check.Source ?? throw new Exception("Source is null"));

        var formatProvider = providerService.GetFormatProvider(report.Format);
        var chapters = await formatProvider.GetChaptersAsync(sourceStream);
        await aiService.WriteComment(issueId, chapters);
    }
}