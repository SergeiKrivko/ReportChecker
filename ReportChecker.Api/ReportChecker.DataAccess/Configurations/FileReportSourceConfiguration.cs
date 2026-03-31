using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class FileReportSourceConfiguration() : BaseReportSourceConfiguration<FileReportSourceEntity>(r => r.FileSource)
{
    public override void Configure(EntityTypeBuilder<FileReportSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.InitialFileId).IsRequired();
        builder.Property(r => r.EntryFilePath);
    }
}