using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class LlmUsageConfiguration : IEntityTypeConfiguration<LlmUsageEntity>
{
    public void Configure(EntityTypeBuilder<LlmUsageEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.ModelId).IsRequired();
        builder.Property(e => e.ReportId).IsRequired();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.FinishedAt).IsRequired();
        builder.Property(e => e.InputTokens).IsRequired();
        builder.Property(e => e.OutputTokens).IsRequired();
        builder.Property(e => e.TotalTokens).IsRequired();
        builder.Property(e => e.TotalRequests).IsRequired();
    }
}