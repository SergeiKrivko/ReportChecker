namespace ReportChecker.Models;

public class UserSubscription
{
    public required Guid Id { get; init; }
    public required Guid PlanId { get; init; }
    public required Guid UserId { get; init; }
    public Guid? PaymentId { get; init; }

    public required decimal DefaultPricePerMonth { get; init; }
    public required decimal Price { get; init; }

    public required DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }
}