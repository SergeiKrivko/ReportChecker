using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
public class SubscriptionsController(
    ISubscriptionService subscriptionService,
    IUserSubscriptionRepository userSubscriptionRepository) : ControllerBase
{
    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<UserSubscriptionsSchema>> GetActiveSubscription(bool checkPayments = false,
        CancellationToken ct = default)
    {
        var userId = User.UserId;

        UserSubscription? active = null;
        if (checkPayments)
            await subscriptionService.CheckPaymentsAsync(User.UserId, ct);

        active ??= await subscriptionService.GetActiveSubscription(userId, ct);
        var futureSubscriptions = await userSubscriptionRepository.GetFutureSubscriptionsAsync(userId, ct);
        var tokensLimit = await subscriptionService.GetTokensLimitAsync(userId, ct);
        var reportsLimit = await subscriptionService.GetReportsLimitAsync(userId, ct);
        return Ok(new UserSubscriptionsSchema
        {
            Active = active,
            Future = futureSubscriptions,
            ResetLimitsAt = await subscriptionService.GetResetLimitsTime(userId, ct),
            TokensLimit = tokensLimit,
            ReportsLimit = reportsLimit,
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreatedSubscription>> CreateSubscription(CreateUserSubscriptionSchema schema,
        CancellationToken ct = default)
    {
        var userId = User.UserId;
        var subscription = await subscriptionService.CreateSubscriptionAsync(userId, schema.OfferId, ct);
        return Ok(subscription);
    }

    [HttpPost("{subscriptionId:guid}/confirm")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> ConfirmSubscription(Guid subscriptionId, ConfirmSubscriptionSchema schema,
        CancellationToken ct)
    {
        var subscription = await userSubscriptionRepository.GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            throw new NotFoundException("Подписка не найдена");
        if (schema.UserId.HasValue && schema.UserId != subscription.UserId)
            throw new BadRequestException("UserId mismatch");
        if (schema.Price.HasValue && decimal.Abs(schema.Price.Value - subscription.Price) > 1e-10M)
            throw new BadRequestException("Price mismatch");

        await subscriptionService.ConfirmSubscriptionAsync(subscriptionId, ct);
        return Ok();
    }

    [HttpGet("{subscriptionId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<UserSubscription>> GetSubscriptionById(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await userSubscriptionRepository.GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            throw new NotFoundException("Подписка не найдена");
        return Ok(subscription);
    }

    [HttpPost("{subscriptionId:guid}/payment")]
    [Authorize]
    public async Task<ActionResult<DownloadUrlResponse>> CreatePayment(Guid subscriptionId,
        [FromBody] PaymentRequestSchema schema, CancellationToken ct = default)
    {
        var url = await subscriptionService.CreatePaymentAsync(subscriptionId, User.UserId, ct);
        return Ok(new DownloadUrlResponse
        {
            Url = url,
        });
    }
}