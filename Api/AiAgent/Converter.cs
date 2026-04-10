using ReportChecker.Models;

namespace AiAgent;

internal static class Converter
{
    public static IAiAgentClient.Chapter ToAgent(this Chapter chapter, ICollection<Issue> issues)
    {
        return new IAiAgentClient.Chapter
        {
            Name = chapter.Name,
            Text = chapter.Content,
            Issues = issues.Where(e => e.Chapter == chapter.Name).Select(e => e.ToAgent()).ToArray(),
        };
    }

    public static IAiAgentClient.Chapter ToAgent(this ChapterDifference chapter, ICollection<Issue> issues)
    {
        return new IAiAgentClient.Chapter
        {
            Name = chapter.Name,
            Text = chapter.Difference,
            Issues = issues.Where(e => e.Chapter == chapter.Name).Select(e => e.ToAgent()).ToArray(),
        };
    }

    public static IAiAgentClient.IssueRead ToAgent(this Issue issue)
    {
        return new IAiAgentClient.IssueRead
        {
            Id = issue.Id,
            Title = issue.Title,
            Comments = issue.Comments
                .Select(e => e.ToAgent())
                .ToArray(),
            Priority = issue.Priority,
        };
    }

    public static IAiAgentClient.CommentRead ToAgent(this Comment comment)
    {
        return new IAiAgentClient.CommentRead
        {
            Id = comment.Id,
            Content = comment.Content,
            Role = comment.UserId == Guid.Empty ? "assistant" : "user",
            Status = comment.Status.ToString(),
            Patch = comment.Patch?.ToAgent(),
        };
    }

    public static IAiAgentClient.PatchLineRead[] ToAgentLines(this string content)
    {
        return content.Split('\n')
            .Select((e, i) => new IAiAgentClient.PatchLineRead
            {
                Content = e,
                Number = i + 1,
            }).ToArray();
    }

    public static PatchLine ToDomain(this IAiAgentClient.PatchLineCreate line,
        IReadOnlyList<IAiAgentClient.PatchLineRead>? previousLines = null)
    {
        var type = Enum.Parse<PatchLineType>(line.Type);
        var previousContent = type == PatchLineType.Add
            ? null
            : previousLines?.FirstOrDefault(e => e.Number == line.Number)?.Content;
        return new PatchLine
        {
            Number = line.Number,
            Content = line.Content,
            PreviousContent = previousContent,
            Type = type,
        };
    }

    public static IAiAgentClient.PatchRead ToAgent(this Patch patch)
    {
        return new IAiAgentClient.PatchRead
        {
            Lines = patch.Lines.Select(e => new IAiAgentClient.PatchLineCreate
            {
                Number = e.Number,
                Content = e.Content ?? "",
                Type = e.Type.ToString(),
            }).ToArray(),
            Status = patch.Status.ToString()
        };
    }
}