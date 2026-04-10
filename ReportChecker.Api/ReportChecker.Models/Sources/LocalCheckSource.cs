namespace ReportChecker.Models.Sources;

public class LocalCheckSource
{
    public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}