using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class PatchLineConfiguration : IEntityTypeConfiguration<PatchLineEntity>
{
    public void Configure(EntityTypeBuilder<PatchLineEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.PatchId).IsRequired();
        builder.Property(e => e.Index).IsRequired();
        builder.Property(e => e.Number).IsRequired();
        builder.Property(e => e.Content);
        builder.Property(e => e.PreviousContent);
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
    }
}