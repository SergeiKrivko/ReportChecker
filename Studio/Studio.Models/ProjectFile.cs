namespace ReportChecker.Studio.Models;

public class ProjectFile : IProjectFileSystemEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public IEnumerable<IProjectFileSystemEntry> Children => [];
}