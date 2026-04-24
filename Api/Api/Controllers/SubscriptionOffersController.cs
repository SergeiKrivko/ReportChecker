using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions/plans/{planId:guid}/offers")]
public class SubscriptionOffersController(ISubscriptionOfferRepository subscriptionOfferRepository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SubscriptionOffer>>> GetAllOffers(Guid planId, CancellationToken ct)
    {
        var offers = await subscriptionOfferRepository.GetAllOffersAsync(planId, ct);
        return Ok(offers);
    }

    [HttpGet("{offerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SubscriptionOffer>> GetAllOffers(Guid planId, Guid offerId,
        CancellationToken ct)
    {
        var offer = await subscriptionOfferRepository.GetOfferById(offerId, ct);
        if (offer == null || offer.PlanId != planId)
            return NotFound();
        return Ok(offer);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreateOffer(Guid planId, CreateSubscriptionOfferSchema schema,
        CancellationToken ct)
    {
        var id = await subscriptionOfferRepository.CreateOfferAsync(planId, schema.Months, schema.Price, ct);
        return Ok(id);
    }

    [HttpPut("{offerId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdateOffer(Guid planId, Guid offerId, CreateSubscriptionOfferSchema schema,
        CancellationToken ct)
    {
        var res = await subscriptionOfferRepository.UpdateOfferAsync(offerId, schema.Months, schema.Price, ct);
        if (!res)
            return NotFound();
        return Ok();
    }

    [HttpDelete("{offerId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteOffer(Guid planId, Guid offerId, CancellationToken ct)
    {
        var res = await subscriptionOfferRepository.DeleteOfferAsync(offerId, ct);
        if (!res)
            return NotFound();
        return Ok();
    }
}