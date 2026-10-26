namespace ReportChecker.Api.Dtos;

public class LocalCheckSourceDto
{
    public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}