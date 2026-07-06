namespace ReportChecker.Studio.Models;

public class UserSubscription
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

}