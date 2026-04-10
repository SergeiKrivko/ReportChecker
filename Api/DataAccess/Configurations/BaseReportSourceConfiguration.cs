using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess.Configurations;

public abstract class BaseReportSourceConfiguration<TEntity>(Expression<Func<ReportEntity, TEntity?>> navigationExpression)
    : IEntityTypeConfiguration<TEntity> where TEntity : BaseReportSourceEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).IsRequired();
        builder.Property(r => r.ReportId).IsRequired();

        builder.HasOne(r => r.Report)
            .WithOne(navigationExpression)
            .HasForeignKey<TEntity>(r => r.ReportId);
    }
}