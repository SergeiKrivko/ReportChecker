using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<CommentEntity>
{
    public void Configure(EntityTypeBuilder<CommentEntity> builder)
    {
        builder.HasKey(x => x.CommentId);

        builder.Property(x => x.CommentId).IsRequired();
        builder.Property(x => x.IssueId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Content);
        builder.Property(x => x.Status);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ModifiedAt);
        builder.Property(x => x.DeletedAt);
    }
}