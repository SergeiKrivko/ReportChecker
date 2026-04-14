namespace ReportChecker.Cli.Models;

public class Check
{
    public required Guid Id { get; init; }
    public required Guid ReportId { get; init; }
    public required ProgressStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public enum ProgressStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}