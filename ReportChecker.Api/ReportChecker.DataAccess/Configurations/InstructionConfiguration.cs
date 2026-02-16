using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class InstructionConfiguration : IEntityTypeConfiguration<InstructionEntity>
{
    public void Configure(EntityTypeBuilder<InstructionEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.ReportId).IsRequired();
        builder.Property(e => e.Content).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.DeletedAt);
    }
}