using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class CheckConfiguration : IEntityTypeConfiguration<CheckEntity>
{
    public void Configure(EntityTypeBuilder<CheckEntity> builder)
    {
        builder.HasKey(x => x.CheckId);

        builder.Property(x => x.CheckId).IsRequired();
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name);
        builder.Property(x => x.Source);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasMany(x => x.Issues)
            .WithOne(x => x.Check)
            .HasForeignKey(x => x.CheckId);
    }
}