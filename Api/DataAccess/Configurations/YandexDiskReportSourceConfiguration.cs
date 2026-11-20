using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class YandexDiskReportSourceConfiguration()
    : BaseReportSourceConfiguration<YandexDiskReportSourceEntity>(r => r.YandexDiskSource)
{
    public override void Configure(EntityTypeBuilder<YandexDiskReportSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.OwnerId).IsRequired();
        builder.Property(r => r.Path).IsRequired();
        builder.Property(r => r.EntryFilePath);
    }
}
