using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReportChecker.Abstractions;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Entities;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.BackgroundServices;

public class MigrationBackgroundService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(5000, ct);
        await using var scope = serviceProvider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<MigrationService>();
        await service.MigrateAsync(ct);
    }
}

public class MigrationService(
    ReportCheckerDbContext dbContext,
    IProviderService providerService,
    ILogger<MigrationService> logger)
{
    public async Task MigrateAsync(CancellationToken ct)
    {
        var reports = await dbContext.Reports
            .Where(e => e.Source != null)
            .ToListAsync(ct);

        foreach (var report in reports)
        {
            await MigrateReport(report, ct);
        }
    }

    private async Task MigrateReport(ReportEntity report, CancellationToken ct)
    {
        if (report.Source == null)
        {
            logger.LogInformation("Skipping report '{id}'", report.ReportId);
            return;
        }
        logger.LogInformation("Processing report '{id}'", report.ReportId);
        var source = ParseOldSource(report.SourceProvider, report.Source);
        if (source == null)
        {
            logger.LogWarning("Report '{id}': source is null", report.ReportId);
            return;
        }
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        await sourceProvider.SaveAsync(report.ReportId, source);
        await dbContext.Reports
            .Where(e => e.ReportId == report.ReportId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Source, (string?)null), ct);

        var checks = await dbContext.Checks
            .Where(e => e.ReportId == report.ReportId)
            .ToListAsync(ct);

        foreach (var check in checks)
        {
            await MigrateCheck(report, check, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task MigrateCheck(ReportEntity report, CheckEntity check, CancellationToken ct)
    {
        if (check.Source == null)
        {
            logger.LogInformation("Skipping check '{id}'", check.CheckId);
            return;
        }
        logger.LogInformation("Processing check '{id}'", check.CheckId);
        var source = ParseOldCheckSource(report.SourceProvider, check.Source);
        if (source == null)
        {
            logger.LogWarning("Check '{id}': source is null", check.CheckId);
            return;
        }
        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        await sourceProvider.SaveAsync(check.CheckId, source);
        await dbContext.Checks
            .Where(e => e.CheckId == check.CheckId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Source, (string?)null), ct);
    }

    private readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private ReportSourceUnion? ParseOldSource(string provider, string source)
    {
        switch (provider)
        {
            case "File":
            {
                var schema = JsonSerializer.Deserialize<OldFileSourceSchema>(source, _camelCase);
                if (schema == null)
                    return null;
                return new ReportSourceUnion
                {
                    File = new FileReportSource
                    {
                        InitialFileId = schema.Id
                    }
                };
            }
            case "GitHub":
            {
                var schema = JsonSerializer.Deserialize<GitHubSourceSchema>(source);
                if (schema == null)
                    return null;
                return new ReportSourceUnion
                {
                    GitHub = new GitHubReportSource
                    {
                        RepositoryId = schema.RepositoryId,
                        Branch = schema.BranchName ?? "",
                        Path = schema.FilePath,
                    }
                };
            }
        }

        return null;
    }

    private CheckSourceUnion? ParseOldCheckSource(string provider, string source)
    {
        switch (provider)
        {
            case "File":
            {
                var schema = JsonSerializer.Deserialize<OldFileSourceSchema>(source, _camelCase);
                if (schema == null)
                    return null;
                return new CheckSourceUnion
                {
                    Id = schema.Id,
                    File = new FileCheckSource
                    {
                        FileName = schema.FileName,
                        CreatedAt = DateTime.UtcNow,
                    }
                };
            }
            case "GitHub":
            {
                var schema = JsonSerializer.Deserialize<GitHubCommitSourceSchema>(source);
                if (schema == null)
                    return null;
                return new CheckSourceUnion
                {
                    GitHub = new GitHubCheckSource
                    {
                        CommitHash = schema.CommitId,
                    }
                };
            }
        }

        return null;
    }

    private class OldFileSourceSchema
    {
        public required Guid Id { get; init; }
        public required string FileName { get; init; }
    }

    private class GitHubSourceSchema
    {
        public required long RepositoryId { get; init; }
        public string? BranchName { get; init; }
        public required string FilePath { get; init; }
    }

    private class GitHubCommitSourceSchema : GitHubSourceSchema
    {
        public required string CommitId { get; init; }
    }
}