using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class BenchmarkRunConfiguration : IEntityTypeConfiguration<BenchmarkRunEntity>
{
    public void Configure(EntityTypeBuilder<BenchmarkRunEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.CaseId).IsRequired();
        builder.Property(x => x.CaseName).IsRequired();
        builder.Property(x => x.ModelId).IsRequired();
        builder.Property(x => x.ReasoningEffort).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.StartedAt);
        builder.Property(x => x.FinishedAt);
        builder.Property(x => x.DeletedAt);
        builder.Property(x => x.FailureReason);
        builder.Property(x => x.TotalCost).HasColumnType("numeric");

        builder.HasIndex(x => new { x.CaseId, x.ModelId, x.CreatedAt });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DeletedAt);

        builder.HasOne(x => x.Model)
            .WithMany()
            .HasForeignKey(x => x.ModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Results)
            .WithOne(x => x.Run)
            .HasForeignKey(x => x.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
