using System.Text.Json.Serialization;
using ReportChecker.Models;

namespace AiAgent.Models;

public class IssueReadAgent
{
    public Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public int Priority { get; init; }

    public required CommentReadAgent[] Comments { get; init; }
}

public class CommentReadAgent
{
    public Guid Id { get; init; }

    public string? Content { get; init; }

    public string? Status { get; init; }

    public required string Role { get; init; }

    public PatchReadAgent? Patch { get; init; }
}

public class IssueCreateAgent
{
    public required string Chapter { get; init; }

    public required string Title { get; init; }

    public int Priority { get; init; }

    public required string Comment { get; init; }

    public ICollection<PatchLineAgent>? Patch { get; init; }
}

public class CommentCreateAgent
{
    public Guid IssueId { get; init; }

    public string? Content { get; init; }

    public string? Status { get; init; }
}

public class ChapterAgent
{
    public required string Name { get; init; }

    public required string Text { get; init; }

    public IssueReadAgent[]? Issues { get; init; }
    [JsonIgnore] public ChapterImage[] Images { get; init; } = [];
    [JsonIgnore] public ImageProcessingMode ImageProcessingMode { get; init; }
}

public class IssuesRequestAgent
{
    public required ChapterAgent[] Chapters { get; init; }

    public string[] Instructions { get; init; } = [];
}

public class WriteCommentRequestAgent
{
    public required string Text { get; init; }

    public required IssueReadAgent Issue { get; init; }

    public string[] Instructions { get; init; } = [];
    [JsonIgnore] public ChapterImage[] Images { get; init; } = [];
    [JsonIgnore] public ImageProcessingMode ImageProcessingMode { get; init; }
}

public class InstructionCreateAgent
{
    public required string InstructionText { get; init; }

    public bool Apply { get; init; }

    public bool Search { get; init; }

    public bool Save { get; init; }
}

public class CommentResponseAgent
{
    public required CommentCreateAgent Comment { get; init; }

    public InstructionCreateAgent? Instruction { get; init; }

    public ICollection<PatchLineAgent>? Patch { get; init; }
}

public class InstructionRequestAgent
{
    public required string Instruction { get; init; }

    public required ChapterAgent[] Chapters { get; init; }
}

public class PatchLineAgent
{
    public int Number { get; init; }

    public string? Content { get; init; }

    public required string Type { get; init; }
}

public class PatchReadAgent
{
    public required string Status { get; init; }

    public required PatchLineAgent[] Lines { get; init; }
}