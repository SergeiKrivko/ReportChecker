using System.Net.Http.Json;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class LimitsService(
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    ICommentRepository commentRepository,
    IHttpClientFactory httpClientFactory) : ILimitsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Auth");

    private async Task<Dictionary<string, SubscriptionLimits>> LoadSubscriptions()
    {
        var resp = await _httpClient.GetAsync("api/v1/service/subscriptions/plans/data");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Dictionary<string, SubscriptionLimits>>() ?? [];
    }

    private async Task<SubscriptionLimits> GetSubscription(HashSet<string> subscriptions)
    {
        Console.WriteLine(string.Join("; ", subscriptions));
        var existingSubscriptions = (await LoadSubscriptions())
            .Where(e => subscriptions.Contains(e.Key))
            .ToList();
        return new SubscriptionLimits
        {
            MaxReports = existingSubscriptions.Max(e => e.Value.MaxReports),
            MaxChecks = existingSubscriptions.Max(e => e.Value.MaxChecks),
            MaxComments = existingSubscriptions.Max(e => e.Value.MaxComments),
        };
    }

    public async Task<Limits> GetLimitsAsync(Guid userId, HashSet<string> subscriptions)
    {
        var limits = await GetSubscription(subscriptions);
        var startDate = DateTime.UtcNow.AddDays(-7);
        return new Limits
        {
            Reports = new Limit<int>
            {
                Current = await reportRepository.CountReportsAsync(userId),
                Maximum = limits.MaxReports,
            },
            Checks = new Limit<int>
            {
                Current = await checkRepository.CountChecksAsync(userId, startDate),
                Maximum = limits.MaxChecks,
            },
            Comments = new Limit<int>
            {
                Current = await commentRepository.CountAiCommentsAsync(userId, startDate),
                Maximum = limits.MaxComments,
            }
        };
    }

    public async Task<bool> CheckReportsLimitAsync(Guid userId, HashSet<string> subscriptions)
    {
        var limits = await GetSubscription(subscriptions);
        var count = await reportRepository.CountReportsAsync(userId);
        return count < limits.MaxReports;
    }

    public async Task<bool> CheckChecksLimitAsync(Guid userId, HashSet<string> subscriptions)
    {
        var limits = await GetSubscription(subscriptions);
        var startDate = DateTime.UtcNow.AddDays(-7);
        var count = await checkRepository.CountChecksAsync(userId, startDate);
        return count < limits.MaxChecks;
    }

    public async Task<bool> CheckCommentsLimitAsync(Guid userId, HashSet<string> subscriptions)
    {
        var limits = await GetSubscription(subscriptions);
        var startDate = DateTime.UtcNow.AddDays(-7);
        var count = await commentRepository.CountAiCommentsAsync(userId, startDate);
        return count < limits.MaxChecks;
    }
}