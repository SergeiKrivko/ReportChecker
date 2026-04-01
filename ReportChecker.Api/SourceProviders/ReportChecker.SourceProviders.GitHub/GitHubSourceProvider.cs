using Microsoft.Extensions.Configuration;
using Octokit;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubSourceProvider(
    GithubService githubService,
    IConfiguration configuration,
    IIssueRepository issueRepository,
    IReportSourceRepository<GitHubReportSource> reportSourceRepository,
    ICheckSourceRepository<GitHubCheckSource> checkSourceRepository) : ISourceProvider
{
    public string Key => "GitHub";

    public async Task<IFileArchive> OpenAsync(Guid reportId, Guid checkId)
    {
        var reportSource = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                           throw new Exception("Report source not found");
        var checkSource = await checkSourceRepository.GetByCheckIdAsync(checkId) ??
                          throw new Exception("Check source not found");
        return await OpenAsync(reportSource.Data, checkSource.Data);
    }

    private async Task<IFileArchive> OpenAsync(GitHubReportSource reportSource, GitHubCheckSource checkSource)
    {
        var client = await githubService.CreateRepositoryClient(reportSource.RepositoryId);
        return new GitHubArchive(client, reportSource.RepositoryId, checkSource.CommitHash,
            reportSource.Path);
    }

    public async Task<IFileArchive> OpenAsync(ReportSourceUnion source)
    {
        if (source.GitHub == null)
            throw new Exception("GitHub source not set");
        var client = await githubService.CreateRepositoryClient(source.GitHub.RepositoryId);
        var branch = await client.Repository.Branch.Get(source.GitHub.RepositoryId, source.GitHub.Branch);
        return new GitHubArchive(client, source.GitHub.RepositoryId, branch.Commit.Sha,
            source.GitHub.Path);
    }

    public async Task<SourceSchema> GetFirstSourceAsync(Guid reportId)
    {
        var source = await reportSourceRepository.GetByReportIdAsync(reportId) ??
                     throw new Exception("Report source not found");
        var client = await githubService.CreateRepositoryClient(source.Data.RepositoryId);
        var branch = await client.Repository.Branch.Get(source.Data.RepositoryId, source.Data.Branch);

        var result = new GitHubCheckSource
        {
            CommitHash = branch.Commit.Sha,
        };

        return new SourceSchema(new CheckSourceUnion { GitHub = result }, branch.Commit.Label);
    }

    public async Task<Guid> SaveAsync(Guid? checkId, CheckSourceUnion source)
    {
        if (source.GitHub == null)
            throw new Exception("GitHub source not set");
        return await checkSourceRepository.CreateAsync(source.Id ?? Guid.NewGuid(), checkId, source.GitHub);
    }

    public async Task<bool> AttachCheckAsync(Guid id, Guid checkId)
    {
        return await checkSourceRepository.AttachAsync(id, checkId);
    }

    public async Task<Guid> SaveAsync(Guid reportId, ReportSourceUnion source)
    {
        if (source.GitHub == null)
            throw new Exception("GitHub source not set");
        return await reportSourceRepository.CreateAsync(reportId, source.GitHub);
    }

    private long GitHubAppId { get; } = long.Parse(configuration["GitHub.AppId"] ?? "0");

    public async Task WriteCheckStatusAsync(Report report, Check check, bool isCompleted)
    {
        var reportSource = await reportSourceRepository.GetByReportIdAsync(report.Id) ??
                           throw new Exception("Report source not found");
        var checkSource = await checkSourceRepository.GetByCheckIdAsync(check.Id) ??
                          throw new Exception("Check source not found");
        var client = await githubService.CreateRepositoryClient(reportSource.Data.RepositoryId);

        var allSuites =
            await client.Check.Suite.GetAllForReference(reportSource.Data.RepositoryId, checkSource.Data.CommitHash);
        _ = allSuites.CheckSuites.FirstOrDefault(e => e.App.Id == GitHubAppId) ??
            await client.Check.Suite.Create(reportSource.Data.RepositoryId,
                new NewCheckSuite(checkSource.Data.CommitHash));

        var checkName = $"ReportChecker - {report.Name}";
        var allChecks =
            await client.Check.Run.GetAllForReference(reportSource.Data.RepositoryId, checkSource.Data.CommitHash);
        var existingCheck = allChecks.CheckRuns.FirstOrDefault(e => e.App.Id == GitHubAppId && e.Name == checkName) ??
                            await client.Check.Run.Create(reportSource.Data.RepositoryId,
                                new NewCheckRun(checkName, checkSource.Data.CommitHash)
                                {
                                    CompletedAt = DateTimeOffset.UtcNow,
                                    Status = CheckStatus.Queued,
                                    Conclusion = CheckConclusion.Neutral,
                                });

        var issues = (await issueRepository.GetAllIssuesOfReportAsync(report.Id))
            .Where(e => e.Status == IssueStatus.Open)
            .ToList();

        await client.Check.Run.Update(reportSource.Data.RepositoryId, existingCheck.Id, new CheckRunUpdate
        {
            DetailsUrl = $"{configuration["Frontend.Url"]}/reports/{report.Id}",
            Status = isCompleted ? CheckStatus.Completed : CheckStatus.InProgress,
            StartedAt = isCompleted ? null : DateTimeOffset.UtcNow,
            CompletedAt = isCompleted ? DateTimeOffset.UtcNow : null,
            Conclusion = CheckConclusion.Success,
            Output = new NewCheckRunOutput("Проверка отчета", $"Найдено {issues.Count} ошибок:\n" +
                                                              $"**{issues.Count(e => e.Priority >= 1 && e.Priority <= 2)}** критических\n" +
                                                              $"**{issues.Count(e => e.Priority >= 3 && e.Priority <= 5)}** средних\n" +
                                                              $"**{issues.Count(e => e.Priority >= 6)}** слабых\n"),
        });
    }
}