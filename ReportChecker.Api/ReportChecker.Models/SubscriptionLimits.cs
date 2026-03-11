namespace ReportChecker.Models;

public class SubscriptionLimits
{
    public required int MaxReports { get; init; }
    public required int MaxChecks { get; init; }
    public required int MaxComments { get; init; }
}