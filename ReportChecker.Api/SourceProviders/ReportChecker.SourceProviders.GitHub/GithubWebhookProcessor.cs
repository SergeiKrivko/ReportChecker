using Microsoft.Extensions.Logging;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using ReportChecker.Abstractions;
using ReportChecker.Models.Sources;

namespace ReportChecker.SourceProviders.GitHub;

public class GithubWebhookProcessor(
    ICheckService checkService,
    IReportRepository reportRepository,
    IReportSourceRepository<GitHubReportSource> reportSourceRepository,
    ILogger<GithubWebhookProcessor> logger)
    : WebhookEventProcessor
{
    protected override async ValueTask ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent,
        CancellationToken ct = default)
    {
        await base.ProcessPushWebhookAsync(headers, pushEvent, ct);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Webhook received: repo '{repo}', commit '{commit}'", pushEvent.Repository?.Name,
                pushEvent.After);

        if (pushEvent.Repository == null)
            return;

        var sources = await reportSourceRepository.GetByExternalIdAsync(pushEvent.Repository.Id, ct);
        foreach (var source in sources)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Adding check to report '{report}'", source.ReportId);

            var report = await reportRepository.GetReportByIdAsync(source.ReportId);
            if (report == null)
                logger.LogWarning("Report '{report}' not found (maybe deleted)", source.ReportId);
            else
                await checkService.CreateCheckAsync(report.Id, report.OwnerId, new CheckSourceUnion
                {
                    GitHub = new GitHubCheckSource
                    {
                        CommitHash = pushEvent.After,
                    }
                });
        }
    }
}