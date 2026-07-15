using AiAgent.Models;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiService(
    IAiAgentFactory aiAgentFactory,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    IInstructionRepository instructionRepository,
    IPatchRepository patchRepository,
    IInstructionTaskRepository instructionTaskRepository,
    IDifferenceService differenceService,
    IChapterGroupService chapterGroupService,
    ILogger<AiService> logger) : IAiService
{
    public async Task FindIssuesAsync(CheckContext context, CancellationToken ct = default)
    {
        await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Check);

        var changedChapters = differenceService
            .GetDifference(context.NewChapters, context.OldChapters)
            .Where(e => e.NewContent != e.OldContent)
            .ToList();
        var instructions = (await instructionRepository.GetInstructionsAsync(context.Report.Id, ct))
            .Select(e => e.Content)
            .ToArray();

        foreach (var comments in await Task.WhenAll(chapterGroupService
                     .GroupChapters(changedChapters.Where(e => e.OldContent != null))
                     .Select(chapterGroup => aiAgentClient.CheckIssues(new IssuesRequestAgent
                     {
                         Chapters = chapterGroup
                             .Select(e => e.ToAgent(context.Issues.Where(x => x.Status == IssueStatus.Open).ToList(),
                                 context.Report.ImageProcessingMode))
                             .ToArray(),
                         Instructions = instructions,
                     }, ct))))
        {
            foreach (var comment in comments ?? [])
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(comment.Content) || comment.Status != null)
                        await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                            comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                    if (comment.Line != null)
                    {
                        var issue = context.Issues.FirstOrDefault(e => e.Id == comment.IssueId);
                        if (issue != null)
                            await issueRepository.UpdateIssueLocationAsync(comment.IssueId, context.Check.Id,
                                issue.Chapter, comment.Line, ct);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Error when processing comment in the AI response: {message}", e.Message);
                }
            }
        }

        foreach (var issues in await Task.WhenAll(chapterGroupService.GroupChapters(changedChapters)
                     .Select(chapterGroup => aiAgentClient.FindIssues(new IssuesRequestAgent
                     {
                         Chapters = chapterGroup
                             .Select(e => e.ToAgent(context.Issues, context.Report.ImageProcessingMode))
                             .ToArray(),
                         Instructions = instructions,
                     }, ct))))
        {
            await ProcessIssuesAsync(context.Check.Id, issues ?? [], context.NewChapters, ct);
        }

        foreach (var chapter in context.OldChapters.Where(e => context.NewChapters.All(x => x.Name != e.Name)))
        {
            foreach (var issue in context.Issues.Where(e => e.Chapter == chapter.Name))
            {
                await commentRepository.CreateCommentAsync(issue.Id, Guid.Empty, $"Глава '{chapter.Name}' удалена",
                    IssueStatus.Closed);
            }
        }
    }

    private async Task ProcessIssuesAsync(Guid checkId, IEnumerable<IssueCreateAgent> issues,
        IReadOnlyCollection<Chapter> chapters, CancellationToken ct = default)
    {
        foreach (var issue in issues)
        {
            try
            {
                var chapter = chapters.First(e => e.Name == issue.Chapter);
                var issueId =
                    await issueRepository.CreateIssueAsync(checkId, issue.Chapter, issue.Line, issue.Title,
                        issue.Priority,
                        ct);
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Adding issue '{title}'", issue.Title);
                var commentId =
                    await commentRepository.CreateCommentAsync(issueId, Guid.Empty, issue.Comment, IssueStatus.Open);
                if (issue.Patch != null)
                {
                    var oldLines = chapter.Content.ToAgentLines();
                    await patchRepository.CreatePatchAsync(commentId, issue.Patch.Select(e => e.ToDomain(oldLines)),
                        PatchStatus.Completed, ct);
                }
            }
            catch (Exception e)
            {
                logger.LogError("Error when processing issue in the AI response: {message}", e.Message);
            }
        }
    }

    public async Task WriteComment(CheckContext context, Issue issue, Guid commentId, CancellationToken ct = default)
    {
        await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Comment);
        var instructions = (await instructionRepository.GetInstructionsAsync(context.Report.Id, ct))
            .Select(e => e.Content)
            .ToArray();

        try
        {
            var chapter = context.NewChapters.First(e => e.Name == issue.Chapter);
            await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.InProgress);
            var resp = await aiAgentClient.WriteComment(new WriteCommentRequestAgent
            {
                Issue = issue.ToAgent(),
                Text = chapter.Content.AddLineNumbers(),
                Instructions = instructions,
                Images = chapter.Images,
                ImageProcessingMode = context.Report.ImageProcessingMode,
            }, ct);
            await commentRepository.FinishCommentAsync(commentId, resp?.Comment.Content,
                resp?.Comment.Status is null ? null : Enum.Parse<IssueStatus>(resp.Comment.Status));
            if (resp?.Patch != null)
            {
                var oldLines = chapter.Content.ToAgentLines();
                await patchRepository.CreatePatchAsync(commentId, resp.Patch.Select(e => e.ToDomain(oldLines)),
                    PatchStatus.Completed, ct);
            }
        }
        catch (Exception)
        {
            await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Failed);
            throw;
        }
    }

    public async Task ProcessInstructionApplyAsync(Guid taskId, CheckContext context, string instruction,
        CancellationToken ct = default)
    {
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress, ct);
        try
        {
            await using var aiAgentClient =
                await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Instruction);
            foreach (var chapterGroup in chapterGroupService.GroupChapters(context.NewChapters))
            {
                var comments = await aiAgentClient.ApplyInstruction(new InstructionRequestAgent
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(context.Issues)).ToArray()
                }, ct);
                foreach (var comment in comments ?? [])
                {
                    await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                        comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                }
            }

            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed, ct);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при применении инструкции к существующим ошибкам:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed, ct);
        }
    }

    public async Task ProcessInstructionSearchAsync(Guid taskId, CheckContext context, string instruction,
        CancellationToken ct = default)
    {
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress, ct);
        try
        {
            await using var aiAgentClient =
                await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Instruction);
            foreach (var chapterGroup in chapterGroupService.GroupChapters(context.NewChapters))
            {
                var newIssues = await aiAgentClient.SearchInstruction(new InstructionRequestAgent
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(context.Issues)).ToArray()
                }, ct);
                await ProcessIssuesAsync(context.Check.Id, newIssues ?? [], context.NewChapters, ct);
            }

            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed, ct);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при поиске новых ошибок по инструкции:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed, ct);
        }
    }

    public async Task ProcessSearchAnyAsync(Guid taskId, CheckContext context, CancellationToken ct = default)
    {
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress, ct);
        try
        {
            await using var aiAgentClient =
                await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Instruction);
            foreach (var chapterGroup in chapterGroupService.GroupChapters(context.NewChapters))
            {
                var newIssues =
                    await aiAgentClient.SearchAny(chapterGroup.Select(c => c.ToAgent(context.Issues)).ToArray(), ct);
                await ProcessIssuesAsync(context.Check.Id, newIssues ?? [], context.NewChapters, ct);
            }

            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed, ct);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при поиске новых ошибок:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed, ct);
        }
    }

    private async Task ProcessInstructionAsync(CheckContext context,
        InstructionCreateAgent instruction,
        Guid commentId)
    {
        try
        {
            if (instruction.Save)
            {
                await instructionRepository.CreateInstructionAsync(context.Report.Id, instruction.InstructionText,
                    Guid.Empty, commentId);
            }

            if (instruction.Apply)
            {
                var taskId = await instructionTaskRepository.CreateAsync(context.Report.Id, instruction.InstructionText,
                    InstructionTaskMode.Apply);
                await ProcessInstructionApplyAsync(taskId, context, instruction.InstructionText);
            }

            if (instruction.Search)
            {
                var taskId = await instructionTaskRepository.CreateAsync(context.Report.Id, instruction.InstructionText,
                    InstructionTaskMode.Search);
                await ProcessInstructionSearchAsync(taskId, context, instruction.InstructionText);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при обработке комментария '{commentId}':\n{error}", commentId, e.ToString());
            if (instruction.Apply || instruction.Search)
                await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Failed);
            throw;
        }

        if (instruction.Apply || instruction.Search)
            await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Completed);
    }
}