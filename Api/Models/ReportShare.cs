namespace ReportChecker.Models;

public class ReportShare
{
    public required Guid ReportId { get; init; }
    public required Guid UserId { get; init; }
    public required ReportAccessLevel AccessLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
