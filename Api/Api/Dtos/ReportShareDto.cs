using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class ReportShareDto
{
    public required Guid UserId { get; init; }
    public required ReportAccessLevel AccessLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? Name { get; init; }
    public string? Login { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
}
