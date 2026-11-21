namespace ReportChecker.Api.Dtos;

public class GoogleDriveCheckSourceDto
{
    public required string FileId { get; init; }
    public required string ContentHash { get; init; }
    public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
