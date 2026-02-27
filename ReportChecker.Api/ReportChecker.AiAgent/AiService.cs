using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Configuration;
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
    ILogger<AiService> logger,
    IConfiguration configuration) : IAiService
{
    private readonly InlineDiffBuilder _diffBuilder = new InlineDiffBuilder(new Differ());

    public async Task FindIssuesAsync(Guid reportId, Guid checkId, IEnumerable<Chapter> chapters,
        List<Chapter> existingChapters,
        List<Issue> existingIssues)
    {
        var chapterRequests = chapters.Select(c =>
        {
            var existingChapter = existingChapters.FirstOrDefault(e => e.Name == c.Name);
            return new ChapterRequest(
                c.Name,
                existingChapter?.Content,
                c.Content,
                GetDifference(existingChapter?.Content ?? "", c.Content),
                existingIssues
                    .Where(e => e.Chapter == c.Name && e.Status == IssueStatus.Open)
                    .Select(IssueToAgent)
                    .ToArray()
            );
        }).Where(e => e.NewText != e.OldText).ToList();
        var instructions = (await instructionRepository.GetInstructionsAsync(reportId))
            .Select(e => e.Content)
            .ToArray();

        foreach (var chapterGroup in
                 GroupChapters(chapterRequests.Where(e => e.OldText != null && e.Issues.Length > 0)))
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Processing issues from {count} chapters", chapterGroup.Length);

            var comments = await aiAgentClient.CheckIssues(new IAiAgentClient.IssuesRequest
            {
                Chapters = ToAiChapters(chapterGroup),
                Instructions = instructions,
            });
            foreach (var comment in comments ?? [])
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Changing issue {id} status to {status}", comment.IssueId, comment.Status);
                await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                    comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
            }
        }

        foreach (var chapterGroup in GroupChapters(chapterRequests))
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug("Processing issues from {count} chapters", chapterGroup.Length);

            var issues = await aiAgentClient.FindIssues(new IAiAgentClient.IssuesRequest
            {
                Chapters = ToAiChapters(chapterGroup),
                Instructions = instructions,
            });
            await ProcessIssuesAsync(checkId, issues ?? []);
        }
    }

    private static IAiAgentClient.Chapter[] ToAiChapters(IEnumerable<ChapterRequest> chapters)
    {
        return chapters.Select(c => new IAiAgentClient.Chapter
        {
            Name = c.Name,
            Text = c.Difference,
            Issues = c.Issues,
        }).ToArray();
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
                Issue = IssueToAgent(issue),
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
                await ProcessInstructionAsync(id, issue, resp.Instruction, chapters);
        }
        catch (Exception)
        {
            await commentRepository.SetProgressStatusAsync(lastCommentId, ProgressStatus.Failed);
            throw;
        }
    }

    private static IAiAgentClient.IssueRead IssueToAgent(Issue issue)
    {
        return new IAiAgentClient.IssueRead
        {
            Id = issue.Id,
            Status = issue.Status.ToString(),
            Priority = issue.Priority,
            Title = issue.Title,
            Comments = issue.Comments.Select(c => new IAiAgentClient.CommentRead
            {
                Id = c.Id,
                Content = c.Content,
                Status = c.Status?.ToString(),
                Role = c.UserId == Guid.Empty ? "assistant" : "user",
            }).ToArray()
        };
    }


    private string GetDifference(string oldText, string newText)
    {
        var diff = _diffBuilder.BuildDiffModel(oldText, newText);

        var result = new StringBuilder();
        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    result.AppendLine($"+ {line.Text}");
                    break;
                case ChangeType.Deleted:
                    result.AppendLine($"- {line.Text}");
                    break;
                case ChangeType.Modified:
                    result.AppendLine($"* {line.Text}");
                    break;
                default:
                    result.AppendLine($"  {line.Text}");
                    break;
            }
        }

        return result.ToString();
    }

    private int MinChapterSize { get; } = int.Parse(configuration["Reports.MinChapterSize"] ?? "50");
    private int MaxRequestSize { get; } = int.Parse(configuration["Reports.MaxRequestSize"] ?? "5000");

    private IEnumerable<ChapterRequest[]> GroupChapters(IEnumerable<ChapterRequest> chapters)
    {
        var lst = new List<ChapterRequest>();
        var length = 0;
        foreach (var chapter in chapters)
        {
            if (chapter.Difference.Length < MinChapterSize)
                continue;
            if (length + chapter.Difference.Length > MaxRequestSize && lst.Count > 0)
            {
                yield return lst.ToArray();
                lst.Clear();
                length = 0;
            }

            lst.Add(chapter);
            length += chapter.Difference.Length;
        }

        if (lst.Count > 0)
            yield return lst.ToArray();
    }

    private IEnumerable<Chapter[]> GroupChapters(IEnumerable<Chapter> chapters)
    {
        var lst = new List<Chapter>();
        var length = 0;
        foreach (var chapter in chapters)
        {
            if (chapter.Content.Length < MinChapterSize)
                continue;
            if (length + chapter.Content.Length > MaxRequestSize && lst.Count > 0)
            {
                yield return lst.ToArray();
                lst.Clear();
                length = 0;
            }

            lst.Add(chapter);
            length += chapter.Content.Length;
        }

        if (lst.Count > 0)
            yield return lst.ToArray();
    }

    private async Task ProcessInstructionAsync(Guid commentId, Issue issue,
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
                var issues = await issueRepository.GetAllIssuesOfCheckAsync(issue.CheckId);
                foreach (var chapterGroup in GroupChapters(chapters))
                {
                    var comments = await aiAgentClient.ApplyInstruction(new IAiAgentClient.InstructionRequest
                    {
                        Instruction = instruction.InstructionText,
                        Chapters = chapterGroup.Select(c => new IAiAgentClient.Chapter
                        {
                            Name = c.Name,
                            Text = c.Content,
                            Issues = issues
                                .Where(e => e.Chapter == c.Name)
                                .Select(IssueToAgent)
                                .ToArray(),
                        }).ToArray()
                    });
                    foreach (var comment in comments ?? [])
                    {
                        await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                            comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                    }
                }
            }
        }
        catch (Exception)
        {
            if (instruction.Apply || instruction.Search)
                await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Failed);
            throw;
        }

        if (instruction.Apply || instruction.Search)
            await commentRepository.SetProgressStatusAsync(commentId, ProgressStatus.Completed);
    }
}