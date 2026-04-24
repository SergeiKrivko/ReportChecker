using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class SubscriptionService(
    IUserSubscriptionRepository userSubscriptionRepository,
    ILlmUsageRepository llmUsageRepository,
    ISubscriptionPlanRepository subscriptionPlanRepository,
    ISubscriptionOfferRepository subscriptionOfferRepository,
    IReportRepository reportRepository,
    IConfiguration configuration) : ISubscriptionService
{
    private const int DaysPerMonth = 30;

    public async Task<UserSubscription?> GetActiveSubscription(Guid userId, CancellationToken ct = default)
    {
        return await userSubscriptionRepository.GetActiveSubscriptionAsync(userId, ct);
    }

    public async Task<Limit<int>> GetTokensLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await userSubscriptionRepository.GetActiveSubscriptionAsync(userId, ct);
        DateTime monthStart;
        int maxTokens;
        if (subscription == null)
        {
            var lastSubscription = await userSubscriptionRepository.GetLastSubscriptionAsync(userId, ct);
            monthStart = lastSubscription?.EndsAt ?? DateTime.UnixEpoch;
            maxTokens = int.Parse(configuration["Subscriptions.Free.Tokens"] ?? "0");
        }
        else
        {
            monthStart = subscription.StartsAt;
            var plan = await subscriptionPlanRepository.GetPlanByIdAsync(subscription.PlanId, ct);
            if (plan == null)
                throw new Exception($"plan '{subscription.PlanId}' not found");
            maxTokens = plan.TokensLimit;
        }

        monthStart =
            monthStart.AddDays(double.Floor((DateTime.UtcNow - monthStart).TotalDays / DaysPerMonth) * DaysPerMonth);
        var usedTokens = await llmUsageRepository.GetTotalUsageAsync(userId, monthStart, ct);
        return new Limit<int>
        {
            Current = usedTokens,
            Maximum = maxTokens,
        };
    }

    public async Task<Limit<int>> GetReportsLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var active = await userSubscriptionRepository.GetActiveSubscriptionAsync(userId, ct);
        var maxReports = int.Parse(configuration["Subscriptions.Free.Reports"] ?? "0");
        if (active != null)
        {
            var plan = await subscriptionPlanRepository.GetPlanByIdAsync(active.PlanId, ct);
            if (plan != null)
                maxReports = plan.ReportsLimit;
        }

        var count = await reportRepository.CountReportsAsync(userId);
        return new Limit<int>
        {
            Current = count,
            Maximum = maxReports,
        };
    }

    public async Task<bool> CheckTokensLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var limit = await GetTokensLimitAsync(userId, ct);
        return limit.Current < limit.Maximum;
    }

    public async Task<bool> CheckReportsLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var count = await reportRepository.CountReportsAsync(userId);
        var maxReports = int.Parse(configuration["Subscriptions.Free.Reports"] ?? "0");
        if (count < maxReports)
            return true;
        var active = await userSubscriptionRepository.GetActiveSubscriptionAsync(userId, ct);
        if (active != null)
        {
            var plan = await subscriptionPlanRepository.GetPlanByIdAsync(active.PlanId, ct);
            if (plan != null)
                maxReports = plan.ReportsLimit;
        }

        return count < maxReports;
    }

    public async Task<CreatedSubscription> CreateSubscriptionAsync(Guid userId, Guid offerId,
        CancellationToken ct = default)
    {
        var offer = await subscriptionOfferRepository.GetOfferById(offerId, ct);
        if (offer == null)
            return ErrorResult($"Offer {offerId} not found");
        if (offer.DeletedAt != null)
            return ErrorResult("Предложение удалено");
        if (offer.Months <= 0)
            return ErrorResult("Количество месяцев должно быть натуральным числом");
        var plan = await subscriptionPlanRepository.GetPlanByIdAsync(offer.PlanId, ct);
        if (plan == null)
            return ErrorResult($"Plan {offer.PlanId} not found");
        if (plan.DeletedAt != null)
            return ErrorResult("Подписка удалена");

        var now = DateTime.UtcNow;
        var defaultPricePerMonth = offer.Price / offer.Months;
        var activeSubscription = await userSubscriptionRepository.GetActiveSubscriptionAsync(userId, ct);
        var futureSubscriptions = await userSubscriptionRepository.GetFutureSubscriptionsAsync(userId, ct);
        UserSubscription? subscription = null;
        if (activeSubscription == null)
        {
            if (futureSubscriptions.Any())
                return ErrorResult("Обнаружены будущие подписки при отсутствии текущей. " +
                                   "Покупка подписки невозможна. Обратитесь в поддержку.");
            subscription = await userSubscriptionRepository.CreateSubscriptionAsync(plan.Id, userId,
                defaultPricePerMonth, offer.Price, now, now.AddDays(offer.Months * DaysPerMonth), ct);
            return new CreatedSubscription
            {
                Subscription = subscription,
            };
        }

        var activePlan = await subscriptionPlanRepository.GetPlanByIdAsync(activeSubscription.PlanId, ct);
        if (activePlan == null)
            return ErrorResult("Active plan not found");
        if (plan.TokensLimit <= activePlan.TokensLimit)
        {
            if ((activeSubscription.EndsAt - now).TotalDays >= DaysPerMonth ||
                futureSubscriptions.Any())
                return ErrorResult("Невозможно купить новую подписку: осталось больше месяца.");
            subscription = await userSubscriptionRepository.CreateSubscriptionAsync(plan.Id, userId,
                defaultPricePerMonth, offer.Price, activeSubscription.EndsAt,
                activeSubscription.EndsAt.AddDays(offer.Months * DaysPerMonth), ct);
            return new CreatedSubscription
            {
                Subscription = subscription,
            };
        }

        var newSubscriptionMonths = offer.Months - 1;
        var limit = await GetTokensLimitAsync(userId, ct);
        var tokensDiscount = (1 - (decimal)limit.Current / limit.Maximum) * activeSubscription.DefaultPricePerMonth;
        var endsAt = now.AddDays(offer.Months * DaysPerMonth);
        decimal monthsDiscount = 0;
        var nextSubscriptions = new List<UserSubscription>();
        if (newSubscriptionMonths == 0)
            subscription = await userSubscriptionRepository.CreateSubscriptionAsync(plan.Id, userId,
                defaultPricePerMonth, offer.Price - tokensDiscount - monthsDiscount, now, endsAt, ct);
        foreach (var futureSubscription in
                 new List<UserSubscription> { activeSubscription }.Concat(futureSubscriptions))
        {
            var futurePlan = await subscriptionPlanRepository.GetPlanByIdAsync(futureSubscription.PlanId, ct);
            if (futurePlan == null)
                return ErrorResult("Future plan not found");
            if (futurePlan.TokensLimit > plan.TokensLimit)
                return ErrorResult("Апгрейд невозможен. В будущем есть более дорогие подписки.");
            var monthsRemaining = futureSubscription.StartsAt > now
                ? (int)((futureSubscription.EndsAt - futureSubscription.StartsAt).TotalDays / DaysPerMonth)
                : (int)((futureSubscription.EndsAt - now).TotalDays / DaysPerMonth);
            if (monthsRemaining < newSubscriptionMonths)
            {
                monthsDiscount += futureSubscription.DefaultPricePerMonth * monthsRemaining;
                newSubscriptionMonths -= monthsRemaining;
            }
            else
            {
                if (newSubscriptionMonths > 0)
                {
                    monthsDiscount += futureSubscription.DefaultPricePerMonth * monthsRemaining;
                    monthsRemaining -= newSubscriptionMonths;
                    subscription = await userSubscriptionRepository.CreateSubscriptionAsync(plan.Id, userId,
                        defaultPricePerMonth, offer.Price - tokensDiscount - monthsDiscount, now, endsAt, ct);
                    newSubscriptionMonths = 0;
                }

                if (monthsRemaining > 0)
                {
                    var next = await userSubscriptionRepository.CloneSubscriptionAsync(futureSubscription,
                        subscription!.Id, endsAt,
                        endsAt.AddDays(monthsRemaining * DaysPerMonth), ct);
                    nextSubscriptions.Add(next);
                }
            }
        }
        if (newSubscriptionMonths > 0)
            subscription = await userSubscriptionRepository.CreateSubscriptionAsync(plan.Id, userId,
                defaultPricePerMonth, offer.Price - tokensDiscount - monthsDiscount, now, endsAt, ct);

        return new CreatedSubscription
        {
            Subscription = subscription,
            MonthsDiscount = monthsDiscount,
            UnusedTokensDiscount = tokensDiscount,
            NextSubscriptions = nextSubscriptions,
        };
    }

    private static CreatedSubscription ErrorResult(string errorMessage)
    {
        return new CreatedSubscription
        {
            ErrorText = errorMessage,
        };
    }

    public async Task ConfirmSubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
    {
        await userSubscriptionRepository.ConfirmSubscriptionAsync(subscriptionId, ct);
    }
}