using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiService(
    IAiAgentClient aiAgentClient,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    ICheckRepository checkRepository,
    IInstructionRepository instructionRepository,
    IInstructionTaskRepository instructionTaskRepository,
    IDifferenceService differenceService,
    IChapterGroupService chapterGroupService,
    ILogger<AiService> logger) : IAiService
{
    public async Task FindIssuesAsync(Guid reportId, Guid checkId, ICollection<Chapter> chapters,
        ICollection<Chapter> existingChapters,
        ICollection<Issue> existingIssues)
    {
        var changedChapters = differenceService
            .GetDifference(chapters, existingChapters)
            .Where(e => e.NewContent != e.OldContent)
            .ToList();
        var instructions = (await instructionRepository.GetInstructionsAsync(reportId))
            .Select(e => e.Content)
            .ToArray();

        foreach (var chapterGroup in
                 chapterGroupService.GroupChapters(changedChapters.Where(e => e.OldContent != null)))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Processing issues from {count} chapters", chapterGroup.Length);

            var comments = await aiAgentClient.CheckIssues(new IAiAgentClient.IssuesRequest
            {
                Chapters = chapterGroup
                    .Select(e => e.ToAgent(existingIssues.Where(x => x.Status == IssueStatus.Open).ToList()))
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

            var issues = await aiAgentClient.FindIssues(new IAiAgentClient.IssuesRequest
            {
                Chapters = chapterGroup.Select(e => e.ToAgent(existingIssues)).ToArray(),
                Instructions = instructions,
            });
            await ProcessIssuesAsync(checkId, issues ?? []);
        }

        foreach (var chapter in existingChapters.Where(e => !chapters.Select(x => x.Name).Contains(e.Name)))
        {
            foreach (var issue in existingIssues.Where(e => e.Chapter == chapter.Name))
            {
                await commentRepository.CreateCommentAsync(issue.Id, Guid.Empty, $"Глава '{chapter.Name}' удалена",
                    IssueStatus.Closed);
            }
        }
    }

    private async Task ProcessIssuesAsync(Guid checkId, IEnumerable<IAiAgentClient.IssueCreate> issues)
    {
        foreach (var issue in issues)
        {
            var issueId =
                await issueRepository.CreateIssueAsync(checkId, issue.Chapter, issue.Title, issue.Priority);
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Adding issue '{title}'", issue.Title);
            await commentRepository.CreateCommentAsync(issueId, Guid.Empty, issue.Comment, IssueStatus.Open);
        }
    }

    public async Task WriteComment(Report report, Guid issueId, List<Chapter> chapters)
    {
        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue is null)
            return;

        var instructions = (await instructionRepository.GetInstructionsAsync(report.Id))
            .Select(e => e.Content)
            .ToArray();

        var lastCommentId = issue.Comments.Last().Id;
        try
        {
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.InProgress);
            var resp = await aiAgentClient.WriteComment(new IAiAgentClient.WriteCommentRequest
            {
                Issue = issue.ToAgent(),
                Text = chapters.First(e => e.Name == issue.Chapter).Content,
                Instructions = instructions,
            });
            var id = await commentRepository.CreateCommentAsync(issueId, Guid.Empty, resp?.Comment.Content,
                resp?.Comment.Status is null ? null : Enum.Parse<IssueStatus>(resp.Comment.Status),
                (resp?.Instruction?.Apply ?? false) || (resp?.Instruction?.Search ?? false)
                    ? ProgressStatus.InProgress
                    : null);
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.Completed);
            if (resp?.Instruction != null)
                await ProcessInstructionAsync(report.Id, id, issue, resp.Instruction, chapters);
        }
        catch (Exception)
        {
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.Failed);
            throw;
        }
    }

    public async Task ProcessInstructionApplyAsync(Guid taskId, Guid reportId, Guid checkId, List<Chapter> chapters,
        string instruction)
    {
        var issues = (await issueRepository.GetAllIssuesOfReportAsync(reportId)).ToList();
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress);
        try
        {
            foreach (var chapterGroup in chapterGroupService.GroupChapters(chapters))
            {
                var comments = await aiAgentClient.ApplyInstruction(new IAiAgentClient.InstructionRequest
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(issues)).ToArray()
                });
                foreach (var comment in comments ?? [])
                {
                    await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                        comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                }

                await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при применении инструкции к существующим ошибкам:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed);
        }
    }

    public async Task ProcessInstructionSearchAsync(Guid taskId, Guid reportId, Guid checkId, List<Chapter> chapters,
        string instruction)
    {
        var issues = (await issueRepository.GetAllIssuesOfReportAsync(reportId)).ToList();
        await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.InProgress);
        try
        {
            foreach (var chapterGroup in chapterGroupService.GroupChapters(chapters))
            {
                var newIssues = await aiAgentClient.SearchInstruction(new IAiAgentClient.InstructionRequest
                {
                    Instruction = instruction,
                    Chapters = chapterGroup.Select(c => c.ToAgent(issues)).ToArray()
                });
                await ProcessIssuesAsync(checkId, newIssues ?? []);

                await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Completed);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка при поиске новых ошибок по инструкции:\n{error}", e.ToString());
            await instructionTaskRepository.SetStatusAsync(taskId, ProgressStatus.Failed);
        }
    }

    private async Task ProcessInstructionAsync(Guid reportId, Guid commentId, Issue issue,
        IAiAgentClient.InstructionCreate instruction,
        List<Chapter> chapters)
    {
        try
        {
            if (instruction.Save)
            {
                var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
                if (check == null)
                    throw new Exception("Report not found");
                await instructionRepository.CreateInstructionAsync(check.ReportId, instruction.InstructionText);
            }

            if (instruction.Apply)
            {
                var taskId = await instructionTaskRepository.CreateAsync(reportId, instruction.InstructionText,
                    InstructionTaskMode.Apply);
                await ProcessInstructionApplyAsync(taskId, reportId, issue.CheckId, chapters,
                    instruction.InstructionText);
            }

            if (instruction.Search)
            {
                var taskId = await instructionTaskRepository.CreateAsync(reportId, instruction.InstructionText,
                    InstructionTaskMode.Search);
                await ProcessInstructionSearchAsync(taskId, reportId, issue.CheckId, chapters,
                    instruction.InstructionText);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Ошибка ри обработке комментария '{commantId}':\n{error}", commentId, e.ToString());
            if (instruction.Apply || instruction.Search)
                await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Failed);
            throw;
        }

        if (instruction.Apply || instruction.Search)
            await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Completed);
    }
}