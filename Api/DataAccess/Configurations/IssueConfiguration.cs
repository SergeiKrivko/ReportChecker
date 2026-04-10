using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<IssueEntity>
{
    public void Configure(EntityTypeBuilder<IssueEntity> builder)
    {
        builder.HasKey(x => x.IssueId);

        builder.Property(x => x.IssueId).IsRequired();
        builder.Property(x => x.CheckId).IsRequired();
        builder.Property(x => x.Title).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.Chapter).IsRequired();

        builder.HasMany(x => x.Comments)
            .WithOne(x => x.Issue)
            .HasForeignKey(x => x.IssueId);
    }
}