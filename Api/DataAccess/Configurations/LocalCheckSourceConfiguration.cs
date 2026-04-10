using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class LocalCheckSourceConfiguration() : BaseCheckSourceConfiguration<LocalCheckSourceEntity>(c => c.LocalSource)
{
    public override void Configure(EntityTypeBuilder<LocalCheckSourceEntity> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.FileName);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.DeletedAt);
    }
}