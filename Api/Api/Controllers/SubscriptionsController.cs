using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
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
    public async Task<ActionResult<UserSubscriptionsDto>> GetActiveSubscription(bool checkPayments = false,
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
        return Ok(new UserSubscriptionsDto
        {
            Active = active?.ToDto(),
            Future = futureSubscriptions.Select(e => e.ToDto()).ToList(),
            ResetLimitsAt = await subscriptionService.GetResetLimitsTime(userId, ct),
            TokensLimit = tokensLimit.ToDto(),
            ReportsLimit = reportsLimit.ToDto(),
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CreatedSubscriptionDto>> CreateSubscription(CreateUserSubscriptionDto dto,
        CancellationToken ct = default)
    {
        var userId = User.UserId;
        var subscription = await subscriptionService.CreateSubscriptionAsync(userId, dto.OfferId, ct);
        return Ok(subscription.ToDto());
    }

    [HttpPost("{subscriptionId:guid}/confirm")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> ConfirmSubscription(Guid subscriptionId, ConfirmSubscriptionDto dto,
        CancellationToken ct)
    {
        var subscription = await userSubscriptionRepository.GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            throw new NotFoundException("Подписка не найдена");
        if (dto.UserId.HasValue && dto.UserId != subscription.UserId)
            throw new BadRequestException("UserId mismatch");
        if (dto.Price.HasValue && decimal.Abs(dto.Price.Value - subscription.Price) > 1e-10M)
            throw new BadRequestException("Price mismatch");

        await subscriptionService.ConfirmSubscriptionAsync(subscriptionId, ct);
        return Ok();
    }

    [HttpGet("{subscriptionId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<UserSubscriptionDto>> GetSubscriptionById(Guid subscriptionId,
        CancellationToken ct)
    {
        var subscription = await userSubscriptionRepository.GetSubscriptionByIdAsync(subscriptionId, ct);
        if (subscription == null)
            throw new NotFoundException("Подписка не найдена");
        return Ok(subscription.ToDto());
    }

    [HttpPost("{subscriptionId:guid}/payment")]
    [Authorize]
    public async Task<ActionResult<DownloadUrlDto>> CreatePayment(Guid subscriptionId,
        [FromBody] PaymentRequestDto dto, CancellationToken ct = default)
    {
        var url = await subscriptionService.CreatePaymentAsync(subscriptionId, User.UserId, ct);
        return Ok(new DownloadUrlDto
        {
            Url = url,
        });
    }
}