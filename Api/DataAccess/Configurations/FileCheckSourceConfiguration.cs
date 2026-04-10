using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class FileCheckSourceConfiguration() : BaseCheckSourceConfiguration<FileCheckSourceEntity>(c => c.FileSource)
{
    public override void Configure(EntityTypeBuilder<FileCheckSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.FileName);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.DeletedAt);
    }
}