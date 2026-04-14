namespace ReportChecker.Cli.Models;

public class Patch
{
    public required Guid Id { get; init; }
    public required PatchStatus Status { get; init; }
    public required string Chapter { get; init; }
    public required IReadOnlyCollection<PatchLine> Lines { get; init; }
}

public enum PatchStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Accepted = 4,
    Rejected = 5,
    Applied = 6,
}