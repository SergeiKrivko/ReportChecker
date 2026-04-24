using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
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
    public async Task<ActionResult<IEnumerable<SubscriptionPlan>>> GetAllPlans(CancellationToken ct)
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
        return Ok(new[] { freeSubscription }.Concat(plans));
    }

    [HttpGet("{planId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SubscriptionPlan>>> GetPlanById(Guid planId, CancellationToken ct)
    {
        var plan = await subscriptionPlanRepository.GetPlanByIdAsync(planId, ct);
        if (plan == null)
            return NotFound();
        return Ok(plan);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreatePlan(CreateSubscriptionPlanSchema schema, CancellationToken ct)
    {
        var id = await subscriptionPlanRepository.CreatePlanAsync(schema.Name, schema.Description, schema.TokensLimit,
            schema.ReportsLimit, schema.IsHidden, ct);
        return Ok(id);
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdatePlan(Guid planId, CreateSubscriptionPlanSchema schema,
        CancellationToken ct)
    {
        var res = await subscriptionPlanRepository.UpdatePlanAsync(planId, schema.Name, schema.Description,
            schema.TokensLimit, schema.ReportsLimit, schema.IsHidden, ct);
        if (!res)
            return NotFound();
        return Ok();
    }

    [HttpDelete("{planId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeletePlan(Guid planId, CancellationToken ct)
    {
        var res = await subscriptionPlanRepository.DeletePlanAsync(planId, ct);
        if (!res)
            return NotFound();
        return Ok();
    }
}