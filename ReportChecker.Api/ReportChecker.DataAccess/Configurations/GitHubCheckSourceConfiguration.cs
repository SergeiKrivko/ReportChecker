using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class GitHubCheckSourceConfiguration() : BaseCheckSourceConfiguration<GitHubCheckSourceEntity>(c => c.GitHubSource)
{
    public override void Configure(EntityTypeBuilder<GitHubCheckSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.CommitHash);
    }
}