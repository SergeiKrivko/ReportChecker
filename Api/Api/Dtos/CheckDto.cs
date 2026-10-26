using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class CheckDto
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public required Guid UserId { get; init; }
    public string? Name { get; init; }
    public DateTime CreatedAt { get; init; }
    public ProgressStatus Status { get; init; }
}