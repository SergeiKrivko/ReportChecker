using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<ReportEntity>
{
    public void Configure(EntityTypeBuilder<ReportEntity> builder)
    {
        builder.HasKey(x => x.ReportId);

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Name);
        builder.Property(x => x.SourceProvider).IsRequired();
        builder.Property(x => x.Source);
        builder.Property(x => x.Format).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasMany(x => x.Checks)
            .WithOne(x => x.Report)
            .HasForeignKey(x => x.ReportId);
    }
}