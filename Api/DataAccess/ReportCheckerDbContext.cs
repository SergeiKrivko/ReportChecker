using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess.Entities;

namespace ReportChecker.DataAccess;

public class ReportCheckerDbContext : DbContext
{
    public DbSet<CheckEntity> Checks { get; init; }
    public DbSet<CommentEntity> Comments { get; init; }
    public DbSet<ReportEntity> Reports { get; init; }
    public DbSet<IssueEntity> Issues { get; init; }
    public DbSet<InstructionEntity> Instructions { get; init; }
    public DbSet<InstructionTaskEntity> InstructionTasks { get; init; }
    public DbSet<CommentReadEntity> CommentReads { get; init; }
    public DbSet<PatchEntity> Patches { get; init; }
    public DbSet<PatchLineEntity> PatchLines { get; init; }
    public DbSet<LlmModelEntity> LlmModels { get; init; }
    public DbSet<LlmUsageEntity> LlmUsages { get; init; }
    public DbSet<SubscriptionPlanEntity> SubscriptionPlans { get; init; }
    public DbSet<SubscriptionOfferEntity> SubscriptionOffers { get; init; }
    public DbSet<UserSubscriptionEntity> UserSubscriptions { get; init; }
    public DbSet<PaymentEntity> Payments { get; init; }

    public DbSet<FileReportSourceEntity> FileReportSources { get; init; }
    public DbSet<FileCheckSourceEntity> FileCheckSources { get; init; }
    public DbSet<LocalReportSourceEntity> LocalReportSources { get; init; }
    public DbSet<LocalCheckSourceEntity> LocalCheckSources { get; init; }
    public DbSet<GitHubReportSourceEntity> GitHubReportSources { get; init; }
    public DbSet<GitHubCheckSourceEntity> GitHubCheckSources { get; init; }

    public ReportCheckerDbContext(DbContextOptions<ReportCheckerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportCheckerDbContext).Assembly);
    }
}