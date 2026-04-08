using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class PatchService(
    IPatchRepository patchRepository,
    IServiceProvider serviceProvider,
    IAiAgentClient aiAgentClient,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    ICheckRepository checkRepository,
    IReportRepository reportRepository,
    IProviderService providerService,
    ILogger<PatchService> logger) : IPatchService
{
    public async Task<Guid> CreatePatchAsync(Guid commentId, Chapter chapter, CancellationToken ct = default)
    {
        var id = await patchRepository.CreatePatchAsync(commentId, ct);
        RunPatch(id, chapter);
        return id;
    }

    private async void RunPatch(Guid patchId, Chapter chapter)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IPatchService>();
            await service.RunPatchAsync(patchId, chapter, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when trying to create patch");
        }
    }

    public async Task RunPatchAsync(Guid patchId, Chapter chapter, CancellationToken ct = default)
    {
        var patch = await patchRepository.GetPatchById(patchId, ct);
        if (patch is null)
            throw new Exception($"Patch {patchId} not found");
        await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.InProgress, ct);

        try
        {
            var comment = await commentRepository.GetCommentByIdAsync(patch.CommentId);
            if (comment is null)
                throw new Exception($"Comment {patch.CommentId} not found");
            var issue = await issueRepository.GetIssueByIdAsync(comment.IssueId);
            if (issue is null)
                throw new Exception($"Issue {comment.IssueId} not found");
            var lines = await aiAgentClient.Patch(new IAiAgentClient.PatchRequest
            {
                Text = chapter.Content.ToAgentLines(),
                Issue = issue.ToAgent(),
            }) ?? [];
            await patchRepository.AddPatchLinesAsync(patchId, lines.Select(e => e.ToDomain()), ct);
            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Completed, ct);
        }
        catch (Exception)
        {
            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Failed, ct);
            throw;
        }
    }

    public async Task SetPatchStatus(Guid patchId, PatchStatus status, CancellationToken ct = default)
    {
        await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Failed, ct);
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
            await service.ApplyPatchAsync(patchId, CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when trying to apply patch");
        }
    }

    public async Task ApplyPatchAsync(Guid patchId, CancellationToken ct = default)
    {
        try
        {
            var patch = await patchRepository.GetPatchById(patchId, ct);
            if (patch is null)
                throw new Exception($"Patch {patchId} not found");
            var comment = await commentRepository.GetCommentByIdAsync(patch.CommentId);
            if (comment is null)
                throw new Exception($"Comment {patch.CommentId} not found");
            var issue = await issueRepository.GetIssueByIdAsync(comment.IssueId);
            if (issue is null)
                throw new Exception($"Issue {comment.IssueId} not found");
            var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
            if (check is null)
                throw new Exception($"Check {issue.CheckId} not found");
            var report = await reportRepository.GetReportByIdAsync(check.ReportId);
            if (report is null)
                throw new Exception($"Report {check.ReportId} not found");
            var latestCheck = await checkRepository.GetLatestCheckOfReportAsync(check.ReportId);
            if (latestCheck is null)
                throw new Exception($"Latest check of report {check.ReportId} not found");

            var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
            var source = await sourceProvider.OpenAsync(report.Id, latestCheck.Id);
            var formatProvider = providerService.GetFormatProvider(report.Format);
            await formatProvider.ApplyPatchAsync(source, issue.Chapter, patch.Lines, ct);

            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Applied, ct);
        }
        catch (Exception)
        {
            await patchRepository.UpdatePatchStatusAsync(patchId, PatchStatus.Failed, ct);
            throw;
        }
    }
}