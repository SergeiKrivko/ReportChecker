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
    public async Task FindIssuesAsync(Guid checkId, IEnumerable<Chapter> chapters, List<Issue> existingIssues)
    {
        foreach (var chapter in chapters)
        {
            logger.LogDebug("Processing chapter {name}", chapter.Name);
            var existingIssuesForChapter = existingIssues
                .Where(e => e.Chapter == chapter.Name && e.Status == IssueStatus.Open)
                .Select(IssueToAgent)
                .ToArray();

            if (existingIssuesForChapter.Length > 0)
            {
                var comments = await aiAgentClient.CheckIssues(new IAiAgentClient.CheckIssuesRequest
                {
                    Text = chapter.Content,
                    Issues = existingIssuesForChapter,
                });
                foreach (var comment in comments ?? [])
                {
                    logger.LogDebug("Changing issue {id} status to {status}", comment.IssueId, comment.Status);
                    await commentRepository.CreateCommentAsync(comment.IssueId, Guid.Empty, comment.Content,
                        comment.Status is null ? null : Enum.Parse<IssueStatus>(comment.Status));
                }
            }

            var issues = await aiAgentClient.FindIssues(new IAiAgentClient.FindIssuesRequest
            {
                Text = chapter.Content,
                ExistingIssues = existingIssuesForChapter,
            });
            foreach (var issue in issues ?? [])
            {
                var issueId =
                    await issueRepository.CreateIssueAsync(checkId, chapter.Name, issue.Title, issue.Priority);
                logger.LogDebug("Adding issue '{title}'", issue.Title);
                await commentRepository.CreateCommentAsync(issueId, Guid.Empty, issue.Comment, IssueStatus.Open);
            }
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
}