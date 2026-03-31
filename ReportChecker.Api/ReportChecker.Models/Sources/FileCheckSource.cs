namespace ReportChecker.Models.Sources;

public class FileCheckSource
{
    public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}