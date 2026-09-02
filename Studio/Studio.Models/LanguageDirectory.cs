namespace ReportChecker.Studio.Models;

public class LanguageDirectory
{
    public required string Name { get; init; }
    public string? IconKey { get; init; }
    public IReadOnlyList<LanguageFile> Files { get; init; } = [];
    public IReadOnlyList<LanguageDirectory> Directories { get; init; } = [];
}