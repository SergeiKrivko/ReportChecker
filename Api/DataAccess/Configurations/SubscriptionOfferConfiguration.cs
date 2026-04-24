using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class SubscriptionOfferConfiguration : IEntityTypeConfiguration<SubscriptionOfferEntity> 
{
    public void Configure(EntityTypeBuilder<SubscriptionOfferEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.PlanId).IsRequired();
        builder.Property(e => e.Months).IsRequired();
        builder.Property(e => e.Price).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.DeletedAt);
    }
}