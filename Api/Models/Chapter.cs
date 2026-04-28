namespace ReportChecker.Models;

public class Chapter
{
    public required string Name { get; init; }
    public required string Content { get; init; }
    public ChapterImage[] Images { get; init; } = [];
    public Chapter[] Children { get; init; } = [];
}