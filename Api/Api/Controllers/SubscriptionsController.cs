using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
public class SubscriptionsController(ILimitsService limitsService) : ControllerBase
{
    [HttpGet("limits")]
    [Authorize]
    public async Task<ActionResult<Limits>> GetLimits(CancellationToken ct = default)
    {
        return Ok(await limitsService.GetLimitsAsync(User.UserId, User.Subscriptions));
    }
}