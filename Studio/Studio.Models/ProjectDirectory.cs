namespace ReportChecker.Studio.Models;

public class ProjectDirectory : IProjectFileSystemEntry
{
    public required string Path { get; init; }
    public required string Name { get; set; }
    public IReadOnlyList<ProjectDirectory> SubDirectories { get; init; } = [];
    public IReadOnlyList<ProjectFile> Files { get; init; } = [];
    public IEnumerable<IProjectFileSystemEntry> Children => SubDirectories
        .Concat(Files.OfType<IProjectFileSystemEntry>());
}