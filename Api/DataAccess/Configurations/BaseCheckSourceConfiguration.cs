using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public abstract class BaseCheckSourceConfiguration<TEntity>(
    Expression<Func<CheckEntity, TEntity?>> navigationExpression)
    : IEntityTypeConfiguration<TEntity> where TEntity : BaseCheckSourceEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired();
        builder.Property(r => r.CheckId);

        builder.HasOne(r => r.Check)
            .WithOne(navigationExpression)
            .HasForeignKey<TEntity>(r => r.CheckId);
    }
}