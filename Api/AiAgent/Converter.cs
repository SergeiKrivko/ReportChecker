using System.Text;
using ReportChecker.Models;

namespace AiAgent;

internal static class Converter
{
    public static IAiAgentClient<string>.Chapter ToAgent(this Chapter chapter, IReadOnlyCollection<Issue> issues)
    {
        return new IAiAgentClient<string>.Chapter
        {
            Name = chapter.Name,
            Text = chapter.Content.AddLineNumbers(),
            Issues = issues.Where(e => e.Chapter == chapter.Name).Select(e => e.ToAgent()).ToArray(),
        };
    }

    public static IAiAgentClient<string>.Chapter ToAgent(this ChapterDifference chapter, IReadOnlyCollection<Issue> issues)
    {
        return new IAiAgentClient<string>.Chapter
        {
            Name = chapter.Name,
            Text = chapter.Difference.ToAgent(),
            Issues = issues.Where(e => e.Chapter == chapter.Name).Select(e => e.ToAgent()).ToArray(),
        };
    }

    private static string ToAgent(this IEnumerable<ChapterLine> lines)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            switch (line.Type)
            {
                case ChapterLineType.Added:
                    builder.AppendLine($"{line.Number:D3} + {line.Content}");
                    break;
                case ChapterLineType.Deleted:
                    builder.AppendLine($"    - {line.Content}");
                    break;
                case ChapterLineType.Modified:
                    builder.AppendLine($"{line.Number:D3} * {line.Content}");
                    break;
                case ChapterLineType.Unchanged:
                    builder.AppendLine($"{line.Number:D3}   {line.Content}");
                    break;
            }
        }

        return builder.ToString();
    }

    public static IAiAgentClient<string>.IssueRead ToAgent(this Issue issue)
    {
        return new IAiAgentClient<string>.IssueRead
        {
            Id = issue.Id,
            Title = issue.Title,
            Comments = issue.Comments
                .Select(e => e.ToAgent())
                .ToArray(),
            Priority = issue.Priority,
        };
    }

    public static IAiAgentClient<string>.CommentRead ToAgent(this Comment comment)
    {
        return new IAiAgentClient<string>.CommentRead
        {
            Id = comment.Id,
            Content = comment.Content,
            Role = comment.UserId == Guid.Empty ? "assistant" : "user",
            Status = comment.Status.ToString(),
            Patch = comment.Patch?.ToAgent(),
        };
    }

    public static IAiAgentClient<string>.PatchLine[] ToAgentLines(this string content)
    {
        return content.Split('\n')
            .Select((e, i) => new IAiAgentClient<string>.PatchLine
            {
                Content = e,
                Number = i + 1,
            }).ToArray();
    }

    public static PatchLine ToDomain(this IAiAgentClient<string>.PatchLine line,
        IReadOnlyList<IAiAgentClient<string>.PatchLine>? previousLines = null)
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

    public static IAiAgentClient<string>.PatchRead ToAgent(this Patch patch)
    {
        return new IAiAgentClient<string>.PatchRead
        {
            Lines = patch.Lines.Select(e => new IAiAgentClient<string>.PatchLine
            {
                Number = e.Number,
                Content = e.Content ?? "",
                Type = e.Type.ToString(),
            }).ToArray(),
            Status = patch.Status.ToString()
        };
    }

    public static string AddLineNumbers(this string content)
    {
        content = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = content.Split('\n');
        return string.Join('\n', lines.Select((e, i) => $"{i + 1:D3} {e}"));
    }
}