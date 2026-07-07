using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class IssueLocationConfiguration : IEntityTypeConfiguration<IssueLocationEntity>
{
    public void Configure(EntityTypeBuilder<IssueLocationEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.IssueId).IsRequired();
        builder.Property(e => e.CheckId).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.Line);
        builder.Property(e => e.Chapter).IsRequired();

        builder.HasOne(e => e.Issue)
            .WithMany(e => e.Locations)
            .HasForeignKey(e => e.IssueId);

        builder.HasOne(e => e.Check)
            .WithMany(e => e.IssueLocations)
            .HasForeignKey(e => e.CheckId);
    }
}