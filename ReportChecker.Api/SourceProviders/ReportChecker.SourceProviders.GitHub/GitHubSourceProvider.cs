using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Octokit;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.SourceProviders.GitHub;

public class GitHubSourceProvider(
    GithubService githubService,
    IConfiguration configuration,
    IIssueRepository issueRepository) : ISourceProvider
{
    public string Key => "GitHub";

    public async Task<IFileArchive> OpenAsync(string sourceJson)
    {
        var source = JsonSerializer.Deserialize<GitHubCommitSourceSchema>(sourceJson) ??
                     throw new InvalidOperationException();
        return await OpenAsync(source);
    }

    private async Task<IFileArchive> OpenAsync(GitHubCommitSourceSchema source)
    {
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);
        var contents =
            await client.Repository.Content.GetAllContentsByRef(source.RepositoryId, source.FilePath, source.CommitId);
        return new GitHubArchive(contents, source.FilePath);
    }

    public async Task<SourceSchema> FindSourceAsync(string sourceJson)
    {
        var source = JsonSerializer.Deserialize<GitHubSourceSchema>(sourceJson) ??
                     throw new InvalidOperationException();
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);
        var branch = await client.Repository.Branch.Get(source.RepositoryId, source.BranchName);

        var result = new GitHubCommitSourceSchema
        {
            RepositoryId = source.RepositoryId,
            BranchName = branch.Name,
            FilePath = source.FilePath,
            CommitId = branch.Commit.Sha
        };

        return new SourceSchema(JsonSerializer.Serialize(result), await OpenAsync(result), branch.Commit.Label);
    }

    private long GitHubAppId { get; } = long.Parse(configuration["GitHub.AppId"] ?? "0");

    public async Task WriteCheckStatusAsync(Report report, Check check, bool isCompleted)
    {
        if (check.Source == null)
            return;
        var source = JsonSerializer.Deserialize<GitHubCommitSourceSchema>(check.Source) ??
                     throw new InvalidOperationException();
        var client = await githubService.CreateRepositoryClient(source.RepositoryId);

        var allSuites = await client.Check.Suite.GetAllForReference(source.RepositoryId, source.CommitId);
        _ = allSuites.CheckSuites.FirstOrDefault(e => e.App.Id == GitHubAppId) ??
            await client.Check.Suite.Create(source.RepositoryId, new NewCheckSuite(source.CommitId));

        var allChecks = await client.Check.Run.GetAllForReference(source.RepositoryId, source.CommitId);
        var existingCheck = allChecks.CheckRuns.FirstOrDefault(e => e.App.Id == GitHubAppId) ??
                            await client.Check.Run.Create(source.RepositoryId,
                                new NewCheckRun("ReportChecker", source.CommitId)
                                {
                                    CompletedAt = DateTimeOffset.UtcNow,
                                    Status = CheckStatus.Queued,
                                    Conclusion = CheckConclusion.Neutral,
                                });

        var issues = (await issueRepository.GetAllIssuesOfReportAsync(check.Id))
            .Where(e => e.Status == IssueStatus.Open)
            .ToList();

        await client.Check.Run.Update(source.RepositoryId, existingCheck.Id, new CheckRunUpdate
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