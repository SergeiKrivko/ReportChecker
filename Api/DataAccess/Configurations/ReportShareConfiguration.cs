using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class ReportShareConfiguration : IEntityTypeConfiguration<ReportShareEntity>
{
    public void Configure(EntityTypeBuilder<ReportShareEntity> builder)
    {
        builder.HasKey(e => new { e.ReportId, e.UserId });

        builder.Property(e => e.ReportId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.AccessLevel).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.UserId);

        builder.HasOne(e => e.Report)
            .WithMany(e => e.Shares)
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
