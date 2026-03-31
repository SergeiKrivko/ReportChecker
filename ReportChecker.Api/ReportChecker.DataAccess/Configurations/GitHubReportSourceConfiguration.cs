using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class GitHubReportSourceConfiguration() : BaseReportSourceConfiguration<GitHubReportSourceEntity>(r => r.GitHubSource)
{
    public override void Configure(EntityTypeBuilder<GitHubReportSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(s => s.RepositoryId).IsRequired();
        builder.Property(s => s.Branch).IsRequired();
        builder.Property(s => s.Path).IsRequired();
    }
}