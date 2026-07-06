namespace ReportChecker.Studio.Models;

public class CurrentSubscription
{
    public UserSubscription? Active { get; set; }
    public DateTimeOffset ResetLimitsAt { get; set; }
    public required Limit<int> TokensLimit { get; set; }
    public required Limit<int> ReportsLimit { get; set; }
}