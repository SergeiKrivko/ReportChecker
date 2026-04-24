using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlanEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.Name).IsRequired();
        builder.Property(e => e.Description);
        builder.Property(e => e.TokensLimit).IsRequired();
        builder.Property(e => e.ReportsLimit).IsRequired();
        builder.Property(e => e.IsHidden).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.DeletedAt);

        builder.HasMany(e => e.Offers)
            .WithOne(e => e.Plan)
            .HasForeignKey(e => e.PlanId);
        builder.HasMany(e => e.UserSubscriptions)
            .WithOne(e => e.Plan)
            .HasForeignKey(e => e.PlanId);
    }
}