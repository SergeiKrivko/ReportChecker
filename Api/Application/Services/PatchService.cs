using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;


namespace ReportChecker.Application.Services;

public class PatchService(
    IPatchRepository patchRepository,
    IServiceProvider serviceProvider,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    ICheckRepository checkRepository,
    IReportRepository reportRepository,
    IProviderService providerService,
    ILogger<PatchService> logger) : IPatchService
{
    public async Task SetPatchStatus(Guid patchId, PatchStatus status, CancellationToken ct = default)
    {
        await patchRepository.UpdatePatchStatusAsync(patchId, status, ct);
        if (status == PatchStatus.Accepted)
        {
            _ApplyPatchAsync(patchId);
        }
    }

    private async void _ApplyPatchAsync(Guid patchId)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IPatchService>();
            var checkService = scope.ServiceProvider.GetRequiredService<ICheckService>();
            await service.ApplyPatchAsync(patchId, checkService, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when trying to apply patch");
        }
    }

    public async Task ApplyPatchAsync(Guid patchId, ICheckService checkService, CancellationToken ct = default)
    {
        try
        {
            var patch = await patchRepository.GetPatchById(patchId, ct);
            if (patch is null)
                throw new NotFoundException($"Patch {patchId} not found");
            var comment = await commentRepository.GetCommentByIdAsync(patch.CommentId);
            if (comment is null)
                throw new NotFoundException($"Comment {patch.CommentId} not found");
            var issue = await issueRepository.GetIssueByIdAsync(comment.IssueId);
            if (issue is null)
                throw new NotFoundException($"Issue {comment.IssueId} not found");
            var check = await checkRepository.GetCheckByIdAsync(issue.CheckId, ct);
            if (check is null)
                throw new NotFoundException($"Check {issue.CheckId} not found");
            var report = await reportRepository.GetReportByIdAsync(check.ReportId);
            if (report is null)
                throw new NotFoundException($"Report {check.ReportId} not found");
            var latestCheck = await checkRepository.GetLatestCheckOfReportAsync(check.ReportId, ct: ct);
            if (latestCheck is null)
                throw new NotFoundException($"Latest check of report {check.ReportId} not found");

            var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
            var source = await sourceProvider.OpenAsync(report.Id, latestCheck.Id);
            if (source.WriteMode == WriteMode.External)
                return;
            if (source.WriteMode == WriteMode.NotSupported)
                throw new NotSupportedBySourceProviderException(sourceProvider);

            var formatProvider = providerService.GetFormatProvider(report.Format);
            var newSource = await formatProvider.ApplyPatchAsync(source, issue.Chapter, patch.Lines, ct);

            if (newSource != null)
                await checkService.CreateCheckAsync(report.Id, report.OwnerId, newSource);

            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Applied, ct);
        }
        catch (Exception)
        {
            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Failed, ct);
            throw;
        }
    }
}