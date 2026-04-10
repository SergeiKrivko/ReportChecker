using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class LocalReportSourceConfiguration() : BaseReportSourceConfiguration<LocalReportSourceEntity>(r => r.LocalSource)
{
    public override void Configure(EntityTypeBuilder<LocalReportSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.InitialFileId).IsRequired();
        builder.Property(r => r.EntryFilePath);
        builder.Property(r => r.ClientId).IsRequired();
        builder.Property(r => r.ClientMachineName);
    }
}