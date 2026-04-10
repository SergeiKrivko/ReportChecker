namespace ReportChecker.Models;

public class ChapterDifference
{
    public required string Name { get; init; }
    public string? OldContent { get; init; }
    public required string NewContent { get; init; }
    public required string Difference { get; init; }
}