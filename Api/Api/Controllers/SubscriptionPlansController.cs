using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions/plans")]
public class SubscriptionPlansController(
    ISubscriptionPlanRepository subscriptionPlanRepository,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetAllPlans(CancellationToken ct)
    {
        var plans = await subscriptionPlanRepository.GetAllPlansAsync(ct);
        var freeSubscription =
            new SubscriptionPlan
            {
                Id = Guid.Empty,
                Name = configuration["Subscriptions.Free.Name"] ?? "Free",
                Description = configuration["Subscriptions.Free.Description"],
                TokensLimit = int.Parse(configuration["Subscriptions.Free.Tokens"] ?? "0"),
                ReportsLimit = int.Parse(configuration["Subscriptions.Free.Reports"] ?? "0"),
                CreatedAt = DateTime.UnixEpoch,
            };
        return Ok(plans.Prepend(freeSubscription).Select(e => e.ToDto()));
    }

    [HttpGet("{planId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SubscriptionPlanDto>> GetPlanById(Guid planId, CancellationToken ct)
    {
        var plan = await subscriptionPlanRepository.GetPlanByIdAsync(planId, ct);
        if (plan == null)
            throw new NotFoundException("План не найден");
        return Ok(plan.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreatePlan(CreateSubscriptionPlanDto dto, CancellationToken ct)
    {
        var id = await subscriptionPlanRepository.CreatePlanAsync(dto.Name, dto.Description, dto.TokensLimit,
            dto.ReportsLimit, dto.IsHidden, ct);
        return Ok(id);
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdatePlan(Guid planId, CreateSubscriptionPlanDto dto,
        CancellationToken ct)
    {
        var res = await subscriptionPlanRepository.UpdatePlanAsync(planId, dto.Name, dto.Description,
            dto.TokensLimit, dto.ReportsLimit, dto.IsHidden, ct);
        if (!res)
            throw new NotFoundException("План не найден");
        return Ok();
    }

    [HttpDelete("{planId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeletePlan(Guid planId, CancellationToken ct)
    {
        var res = await subscriptionPlanRepository.DeletePlanAsync(planId, ct);
        if (!res)
            throw new NotFoundException("План не найден");
        return Ok();
    }
}