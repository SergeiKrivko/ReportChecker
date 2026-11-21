using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class GoogleDriveReportSourceConfiguration()
    : BaseReportSourceConfiguration<GoogleDriveReportSourceEntity>(r => r.GoogleDriveSource)
{
    public override void Configure(EntityTypeBuilder<GoogleDriveReportSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.OwnerId).IsRequired();
        builder.Property(r => r.FileId).IsRequired();
        builder.Property(r => r.Name).IsRequired();
        builder.Property(r => r.EntryFilePath);
    }
}
