namespace ReportChecker.Studio.Models;

public interface IProjectFileSystemEntry
{
    public string Path { get; }
    public string Name { get; }
    public IEnumerable<IProjectFileSystemEntry> Children { get; }
}