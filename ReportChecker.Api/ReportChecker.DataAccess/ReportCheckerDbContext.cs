using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess;

public class ReportCheckerDbContext : DbContext
{
    public DbSet<AccountEntity> Accounts { get; init; }
    public DbSet<CheckEntity> Checks { get; init; }
    public DbSet<CommentEntity> Comments { get; init; }
    public DbSet<ReportEntity> Reports { get; init; }
    public DbSet<IssueEntity> Issues { get; init; }
    public DbSet<UserEntity> Users { get; init; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; init; }

    public ReportCheckerDbContext(DbContextOptions<ReportCheckerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportCheckerDbContext).Assembly);
    }
}