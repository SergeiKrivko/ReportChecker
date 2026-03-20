using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class InstructionTaskConfiguration : IEntityTypeConfiguration<InstructionTaskEntity>
{
    public void Configure(EntityTypeBuilder<InstructionTaskEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Instruction).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}