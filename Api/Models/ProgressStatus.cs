namespace ReportChecker.Models;

public enum ProgressStatus
{
    Queued = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    CancellationRequested = 5,
}