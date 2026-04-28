using System.Text;
using AiAgent.Models;
using ReportChecker.Models;

namespace AiAgent;

internal static class Converter
{
    public static ChapterAgent ToAgent(this Chapter chapter, IReadOnlyCollection<Issue> issues)
    {
        return new ChapterAgent
        {
            Name = chapter.Name,
            Text = chapter.Content.AddLineNumbers(),
            Issues = issues.Where(e => e.Chapter == chapter.Name).Select(e => e.ToAgent()).ToArray(),
        };
    }

    public static ChapterAgent ToAgent(this ChapterDifference chapter, IReadOnlyCollection<Issue> issues)
    {
        return new ChapterAgent
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

    public static IssueReadAgent ToAgent(this Issue issue)
    {
        return new IssueReadAgent
        {
            Id = issue.Id,
            Title = issue.Title,
            Comments = issue.Comments
                .Select(e => e.ToAgent())
                .ToArray(),
            Priority = issue.Priority,
            Status = issue.Status.ToString()
        };
    }

    public static CommentReadAgent ToAgent(this Comment comment)
    {
        return new CommentReadAgent
        {
            Id = comment.Id,
            Content = comment.Content,
            Role = comment.UserId == Guid.Empty ? "assistant" : "user",
            Status = comment.Status.ToString(),
            Patch = comment.Patch?.ToAgent(),
        };
    }

    public static PatchLine[] ToAgentLines(this string content)
    {
        return content.Split('\n')
            .Select((e, i) => new PatchLine
            {
                Content = e,
                Number = i + 1,
            }).ToArray();
    }

    public static PatchLine ToDomain(this PatchLineAgent line,
        IReadOnlyList<PatchLine>? previousLines = null)
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

    public static PatchReadAgent ToAgent(this Patch patch)
    {
        return new PatchReadAgent
        {
            Lines = patch.Lines.Select(e => new PatchLineAgent
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