using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class PatchConfiguration : IEntityTypeConfiguration<PatchEntity>
{
    public void Configure(EntityTypeBuilder<PatchEntity> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).IsRequired();
        builder.Property(p => p.CommentId).IsRequired();
        builder.Property(p => p.Status).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasOne(p => p.Comment)
            .WithOne(c => c.Patch)
            .HasForeignKey<PatchEntity>(p => p.CommentId);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.Patch)
            .HasForeignKey(e => e.PatchId);
    }
}