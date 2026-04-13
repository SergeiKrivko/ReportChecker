namespace ReportChecker.Models;

public class CheckContext
{
    public required Report Report { get; init; }
    public required Check Check { get; init; }
    public required IReadOnlyCollection<Chapter> NewChapters { get; init; }
    public IReadOnlyCollection<Chapter> OldChapters { get; init; } = [];
    public IReadOnlyCollection<Issue> Issues { get; init; } = [];
}