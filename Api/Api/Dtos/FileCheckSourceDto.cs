namespace ReportChecker.Api.Dtos;

public class FileCheckSourceDto
{
    public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}