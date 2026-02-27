using System.Text.Json;
using Microsoft.Extensions.Logging;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.GitHub;

public class GithubWebhookProcessor(
    ICheckService checkService,
    IReportRepository reportRepository,
    ILogger<GithubWebhookProcessor> logger)
    : WebhookEventProcessor
{
    protected override async ValueTask ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent,
        CancellationToken cancellationToken = new())
    {
        await base.ProcessPushWebhookAsync(headers, pushEvent, cancellationToken);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Webhook received: repo '{repo}', commit '{commit}'", pushEvent.Repository?.Name,
                pushEvent.Ref);

        var reports = await reportRepository.GetAllReportsOfSourceAsync("GitHub");
        foreach (var report in reports)
        {
            if (report.Source == null)
                continue;
            var source = JsonSerializer.Deserialize<GitHubSourceSchema>(report.Source);
            if (source == null || source.RepositoryId != pushEvent.Repository?.Id)
                continue;
            // Check branch

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Adding check to report '{report}'", report.Id);

            await checkService.CreateCheckAsync(Guid.Empty, Guid.Empty, JsonSerializer.Serialize(
                new GitHubCommitSourceSchema
                {
                    RepositoryId = source.RepositoryId,
                    BranchName = source.BranchName,
                    FilePath = source.FilePath,
                    CommitId = pushEvent.Ref
                }));
        }
    }
}