namespace ReportChecker.Studio.Models;

public class LanguageFile
{
    public required string Name { get; init; }
    public required string? Path { get; init; }
    public string? IconKey { get; init; }
}