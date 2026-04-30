namespace ReportChecker.DataAccess.Entities;

public class UserSubscriptionEntity
{
    public required Guid Id { get; init; }
    public required Guid PlanId { get; init; }
    public required Guid UserId { get; init; }
    public Guid? LinkedSubscriptionId { get; init; }
    public Guid? ParentSubscriptionId { get; init; }
    public Guid? PaymentId { get; init; }

    public required decimal DefaultPricePerMonth { get; init; }
    public required decimal Price { get; init; }

    public required DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public required DateTime StartsAt { get; init; }
    public required DateTime EndsAt { get; init; }

    public virtual SubscriptionPlanEntity Plan { get; init; } = null!;
    public virtual UserSubscriptionEntity? LinkedSubscription { get; init; }
    public virtual UserSubscriptionEntity? ParentSubscription { get; init; }

    public virtual ICollection<UserSubscriptionEntity> LinkedSubscriptions { get; init; } = [];
    public virtual ICollection<UserSubscriptionEntity> ChildrenSubscriptions { get; init; } = [];
    
    public virtual PaymentEntity? Payment { get; init; }
}