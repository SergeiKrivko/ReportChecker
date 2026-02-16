using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiService(
    IAiAgentClient aiAgentClient,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    ILogger<AiService> logger) : IAiService
{
    private readonly InlineDiffBuilder _diffBuilder = new InlineDiffBuilder(new Differ());

    public async Task FindIssuesAsync(Guid checkId, IEnumerable<Chapter> chapters, List<Chapter> existingChapters,
        List<Issue> existingIssues)
    {
        foreach (var chapter in chapters)
        {
            logger.LogDebug("Processing chapter {name}", chapter.Name);
            var existingIssuesForChapter = existingIssues
                .Where(e => e.Chapter == chapter.Name && e.Status == IssueStatus.Open)
                .Select(IssueToAgent)
                .ToArray();
            var existingChapter = existingChapters.FirstOrDefault(e => e.Name == chapter.Name);

            if (existingChapter is not null)
            {
                if (existingChapter.Content == chapter.Content)
                    continue;
                var diff = GetDifference(existingChapter.Content, chapter.Content);
                var comments = await aiAgentClient.CheckIssues(new IAiAgentClient.CheckIssuesRequest
                {
                    Text = diff,
                    Issues = existingIssuesForChapter,
                });
                foreach (var comment in comments ?? [])
                {
                    logger.LogDebug("Changing issue {id} status to {status}", comment.IssueId, comment.Status);
                    await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                        comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                }

                var issues = await aiAgentClient.FindNewIssues(new IAiAgentClient.FindIssuesRequest
                {
                    Text = diff,
                    ExistingIssues = existingIssuesForChapter,
                });
                await ProcessIssuesAsync(checkId, chapter.Name, issues ?? []);
            }
            else
            {
                var issues = await aiAgentClient.FindInitialIssues(new IAiAgentClient.FindIssuesRequest
                {
                    Text = chapter.Content,
                    ExistingIssues = [],
                });
                await ProcessIssuesAsync(checkId, chapter.Name, issues ?? []);
            }
        }
    }

    private async Task ProcessIssuesAsync(Guid checkId, string chapterName,
        IEnumerable<IAiAgentClient.IssueCreate> issues)
    {
        foreach (var issue in issues)
        {
            var issueId =
                await issueRepository.CreateIssueAsync(checkId, chapterName, issue.Title, issue.Priority);
            logger.LogDebug("Adding issue '{title}'", issue.Title);
            await commentRepository.CreateCommentAsync(issueId, Guid.Empty, issue.Comment, IssueStatus.Open);
        }
    }

    public async Task WriteComment(Guid issueId, IEnumerable<Chapter> chapters)
    {
        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue is null)
            return;
        var resp = await aiAgentClient.WriteComment(new IAiAgentClient.WriteCommentRequest
        {
            Issue = IssueToAgent(issue),
            Text = chapters.First(e => e.Name == issue.Chapter).Content,
        });
        await commentRepository.CreateCommentAsync(issueId, Guid.Empty, resp?.Content,
            resp?.Status is null ? null : Enum.Parse<IssueStatus>(resp.Status));
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


    public string GetDifference(string oldText, string newText)
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
}