using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<UserSubscriptionEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.PlanId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.LinkedSubscriptionId);
        builder.Property(e => e.ParentSubscriptionId);

        builder.Property(e => e.DefaultPricePerMonth).IsRequired();
        builder.Property(e => e.Price).IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.StartsAt).IsRequired();
        builder.Property(e => e.EndsAt).IsRequired();
        builder.Property(e => e.ConfirmedAt);
        builder.Property(e => e.DeletedAt);

        builder.HasOne(e => e.LinkedSubscription)
            .WithMany(e => e.LinkedSubscriptions)
            .HasForeignKey(e => e.LinkedSubscriptionId);

        builder.HasOne(e => e.ParentSubscription)
            .WithMany(e => e.ChildrenSubscriptions)
            .HasForeignKey(e => e.ParentSubscriptionId);
    }
}