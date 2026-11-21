using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class GoogleDriveCheckSourceConfiguration()
    : BaseCheckSourceConfiguration<GoogleDriveCheckSourceEntity>(c => c.GoogleDriveSource)
{
    public override void Configure(EntityTypeBuilder<GoogleDriveCheckSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.FileId).IsRequired();
        builder.Property(c => c.ContentHash).IsRequired();
        builder.Property(c => c.FileName);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.DeletedAt);
    }
}
