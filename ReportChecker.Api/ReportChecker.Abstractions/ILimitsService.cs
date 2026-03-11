using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ILimitsService
{
    public Task<Limits> GetLimitsAsync(Guid userId, HashSet<string> subscriptions);
    public Task<bool> CheckReportsLimitAsync(Guid userId, HashSet<string> subscriptions);
    public Task<bool> CheckChecksLimitAsync(Guid userId, HashSet<string> subscriptions);
    public Task<bool> CheckCommentsLimitAsync(Guid userId, HashSet<string> subscriptions);
}