using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class CommentReadConfiguration : IEntityTypeConfiguration<CommentReadEntity>
{
    public void Configure(EntityTypeBuilder<CommentReadEntity> builder)
    {
        builder.HasKey(e => new { e.CommentId, e.UserId });

        builder.Property(e => e.CommentId).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.Comment)
            .WithMany(e => e.Reads)
            .HasForeignKey(e => e.CommentId);
    }
}