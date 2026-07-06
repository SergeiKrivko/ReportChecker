namespace ReportChecker.Studio.Models;

public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int TokensLimit { get; set; }
    public int ReportsLimit { get; set; }
}