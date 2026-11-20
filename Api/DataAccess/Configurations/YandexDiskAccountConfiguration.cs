using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class YandexDiskAccountConfiguration : IEntityTypeConfiguration<YandexDiskAccountEntity>
{
    public void Configure(EntityTypeBuilder<YandexDiskAccountEntity> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Login);
        builder.Property(x => x.AccessToken).IsRequired();
        builder.Property(x => x.RefreshToken);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
