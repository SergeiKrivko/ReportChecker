using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class LlmModelConfiguration : IEntityTypeConfiguration<LlmModelEntity>
{
    public void Configure(EntityTypeBuilder<LlmModelEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.DisplayName).IsRequired();
        builder.Property(e => e.ModelKey).IsRequired();
        builder.Property(e => e.InputCoefficient).IsRequired();
        builder.Property(e => e.OutputCoefficient).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.DeletedAt);

        builder.HasMany(e => e.Reports)
            .WithOne(e => e.LlmModel)
            .HasForeignKey(e => e.LlmModelId);
        builder.HasMany(e => e.Usages)
            .WithOne(e => e.Model)
            .HasForeignKey(e => e.ModelId);
    }
}