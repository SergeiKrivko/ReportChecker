using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class YandexDiskCheckSourceConfiguration()
    : BaseCheckSourceConfiguration<YandexDiskCheckSourceEntity>(c => c.YandexDiskSource)
{
    public override void Configure(EntityTypeBuilder<YandexDiskCheckSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.Path).IsRequired();
        builder.Property(c => c.ContentHash).IsRequired();
        builder.Property(c => c.FileName);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.DeletedAt);
    }
}
