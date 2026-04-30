using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
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
    public async Task<ActionResult<UserSubscriptionsSchema>> GetActiveSubscription(CancellationToken ct = default)
    {
        var userId = User.UserId;
        var active = await subscriptionService.GetActiveSubscription(userId, ct);
        var futureSubscriptions = await userSubscriptionRepository.GetFutureSubscriptionsAsync(userId, ct);
        var tokensLimit = await subscriptionService.GetTokensLimitAsync(userId, ct);
        var reportsLimit = await subscriptionService.GetReportsLimitAsync(userId, ct);
        return Ok(new UserSubscriptionsSchema
        {
            Active = active,
            Future = futureSubscriptions,
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
            return NotFound();
        if (schema.UserId.HasValue && schema.UserId != subscription.UserId)
            return BadRequest("UserId mismatch");
        if (schema.Price.HasValue && decimal.Abs(schema.Price.Value - subscription.Price) > 1e-10M)
            return BadRequest("Price mismatch");

        await subscriptionService.ConfirmSubscriptionAsync(subscriptionId, ct);
        return Ok();
    }

    [HttpGet("{subscriptionId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<UserSubscription>> GetSubscriptionById(Guid subscriptionId, CancellationToken ct)
    {
        var subscription = await userSubscriptionRepository.GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            return NotFound();
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

    [HttpGet("checkPayments")]
    [Authorize]
    public async Task<ActionResult<UserSubscription>> CheckPayments(CancellationToken ct = default)
    {
        var subscription = await subscriptionService.CheckPaymentsAsync(User.UserId, ct);
        if (subscription == null)
            return NotFound();
        return Ok(subscription);
    }
}