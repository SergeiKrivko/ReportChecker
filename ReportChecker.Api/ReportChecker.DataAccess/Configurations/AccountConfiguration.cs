using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<AccountEntity>
{
    public void Configure(EntityTypeBuilder<AccountEntity> builder)
    {
        builder.HasKey(x => x.AccountId);

        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Provider).IsRequired();
        builder.Property(x => x.ProviderUserId).IsRequired();
        builder.Property(x => x.Credentials).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.DeletedAt);
    }
}