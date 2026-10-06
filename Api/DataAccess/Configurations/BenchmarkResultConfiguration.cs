using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class BenchmarkResultConfiguration : IEntityTypeConfiguration<BenchmarkResultEntity>
{
    public void Configure(EntityTypeBuilder<BenchmarkResultEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RunId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpectedNumber);
        builder.Property(x => x.ErrorClass);
        builder.Property(x => x.Chapter);
        builder.Property(x => x.Line);
        builder.Property(x => x.ExpectedTitle);
        builder.Property(x => x.ExpectedComment);
        builder.Property(x => x.ExpectedPriority);
        builder.Property(x => x.FoundIndex);
        builder.Property(x => x.FoundTitle);
        builder.Property(x => x.FoundComment);
        builder.Property(x => x.FoundPriority);
        builder.Property(x => x.IsFound).IsRequired();
        builder.Property(x => x.TitleMatch);
        builder.Property(x => x.PriorityMatch);
        builder.Property(x => x.PriorityDelta);
        builder.Property(x => x.MatchingMethod).IsRequired();
        builder.Property(x => x.MatchScore);
        builder.Property(x => x.MatchingReason);
        builder.Property(x => x.FixMatchStatus).IsRequired();
        builder.Property(x => x.ExpectedFix);
        builder.Property(x => x.FoundFix);

        builder.HasIndex(x => x.RunId);
    }
}
