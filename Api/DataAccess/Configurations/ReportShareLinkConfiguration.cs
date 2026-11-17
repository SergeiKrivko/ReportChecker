using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class ReportShareLinkConfiguration : IEntityTypeConfiguration<ReportShareLinkEntity>
{
    public void Configure(EntityTypeBuilder<ReportShareLinkEntity> builder)
    {
        builder.HasKey(e => e.ReportId);

        builder.Property(e => e.ReportId).IsRequired();
        builder.Property(e => e.Token).IsRequired().HasMaxLength(64);
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.Token).IsUnique();

        builder.HasOne(e => e.Report)
            .WithOne(e => e.ShareLink)
            .HasForeignKey<ReportShareLinkEntity>(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
