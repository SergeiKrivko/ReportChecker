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
    public async Task FindIssuesAsync(CheckContext context)
    {
        await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Check);

        var changedChapters = differenceService
            .GetDifference(context.NewChapters, context.OldChapters)
            .Where(e => e.NewContent != e.OldContent)
            .ToList();
        var instructions = (await instructionRepository.GetInstructionsAsync(context.Report.Id))
            .Select(e => e.Content)
            .ToArray();

        foreach (var chapterGroup in
                 chapterGroupService.GroupChapters(changedChapters.Where(e => e.OldContent != null)))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Processing issues from {count} chapters", chapterGroup.Length);

            var comments = await aiAgentClient.CheckIssues(new IAiAgentClient<string>.IssuesRequest
            {
                Chapters = chapterGroup
                    .Select(e => e.ToAgent(context.Issues.Where(x => x.Status == IssueStatus.Open).ToList()))
                    .ToArray(),
                Instructions = instructions,
            });
            foreach (var comment in comments ?? [])
            {
                await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                    comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
            }
        }

        foreach (var chapterGroup in chapterGroupService.GroupChapters(changedChapters))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Processing issues from {count} chapters", chapterGroup.Length);

            var issues = await aiAgentClient.FindIssues(new IAiAgentClient<string>.IssuesRequest
            {
                Chapters = chapterGroup.Select(e => e.ToAgent(context.Issues)).ToArray(),
                Instructions = instructions,
            });
            await ProcessIssuesAsync(context.Check.Id, issues ?? [], context.NewChapters);
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

    private async Task ProcessIssuesAsync(Guid checkId, IEnumerable<IAiAgentClient<string>.IssueCreate> issues,
        IReadOnlyCollection<Chapter> chapters)
    {
        foreach (var issue in issues)
        {
            var chapter = chapters.First(e => e.Name == issue.Chapter);
            var issueId =
                await issueRepository.CreateIssueAsync(checkId, issue.Chapter, issue.Title, issue.Priority);
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Adding issue '{title}'", issue.Title);
            var commentId =
                await commentRepository.CreateCommentAsync(issueId, Guid.Empty, issue.Comment, IssueStatus.Open);
            if (issue.Patch != null)
            {
                var oldLines = chapter.Content.ToAgentLines();
                await patchRepository.CreatePatchAsync(commentId, issue.Patch.Select(e => e.ToDomain(oldLines)),
                    PatchStatus.Completed);
            }
        }
    }

    public async Task WriteComment(CheckContext context, Issue issue)
    {
        await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Comment);
        var instructions = (await instructionRepository.GetInstructionsAsync(context.Report.Id))
            .Select(e => e.Content)
            .ToArray();

        var lastCommentId = issue.Comments.Last().Id;
        try
        {
            var chapter = context.NewChapters.First(e => e.Name == issue.Chapter);
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.InProgress);
            var resp = await aiAgentClient.WriteComment(new IAiAgentClient<string>.WriteCommentRequest
            {
                Issue = issue.ToAgent(),
                Text = chapter.Content.AddLineNumbers(),
                Instructions = instructions,
            });
            var id = await commentRepository.CreateCommentAsync(issue.Id, Guid.Empty, resp?.Comment.Content,
                resp?.Comment.Status is null ? null : Enum.Parse<IssueStatus>(resp.Comment.Status),
                (resp?.Instruction?.Apply ?? false) || (resp?.Instruction?.Search ?? false)
                    ? ProgressStatus.InProgress
                    : null);
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.Completed);
            if (resp?.Instruction != null)
                await ProcessInstructionAsync(context, resp.Instruction, id);
            if (resp?.Patch != null)
            {
                var oldLines = chapter.Content.ToAgentLines();
                await patchRepository.CreatePatchAsync(id, resp.Patch.Select(e => e.ToDomain(oldLines)),
                    PatchStatus.Completed);
            }
        }
        catch (Exception)
        {
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.Failed);
            throw;
        }
    }

    public async Task ProcessInstructionApplyAsync(Guid taskId, CheckContext context, string instruction)
    {
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress);
        try
        {
            await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Instruction);
            foreach (var chapterGroup in chapterGroupService.GroupChapters(context.NewChapters))
            {
                var comments = await aiAgentClient.ApplyInstruction(new IAiAgentClient<string>.InstructionRequest
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(context.Issues)).ToArray()
                });
                foreach (var comment in comments ?? [])
                {
                    await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                        comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                }
            }

            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при применении инструкции к существующим ошибкам:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed);
        }
    }

    public async Task ProcessInstructionSearchAsync(Guid taskId, CheckContext context, string instruction)
    {
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress);
        try
        {
            await using var aiAgentClient = await aiAgentFactory.CreateClientAsync(context.Report, LlmUsageType.Instruction);
            foreach (var chapterGroup in chapterGroupService.GroupChapters(context.NewChapters))
            {
                var newIssues = await aiAgentClient.SearchInstruction(new IAiAgentClient<string>.InstructionRequest
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(context.Issues)).ToArray()
                });
                await ProcessIssuesAsync(context.Check.Id, newIssues ?? [], context.NewChapters);
            }

            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed);
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при поиске новых ошибок по инструкции:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed);
        }
    }

    private async Task ProcessInstructionAsync(CheckContext context,
        IAiAgentClient<string>.InstructionCreate instruction,
        Guid commentId)
    {
        try
        {
            if (instruction.Save)
            {
                await instructionRepository.CreateInstructionAsync(context.Report.Id, instruction.InstructionText);
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