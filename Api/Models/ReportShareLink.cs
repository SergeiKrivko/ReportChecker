namespace ReportChecker.Models;

public class ReportShareLink
{
    public required Guid ReportId { get; init; }
    public required string Token { get; init; }
    public DateTime CreatedAt { get; init; }
}
