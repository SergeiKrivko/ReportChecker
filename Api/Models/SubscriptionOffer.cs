namespace ReportChecker.Models;

public class SubscriptionOffer
{
    public required Guid Id { get; init; }
    public required Guid PlanId { get; init; }
    public required int Months { get; init; }
    public required decimal Price { get; init; }

    public required DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}