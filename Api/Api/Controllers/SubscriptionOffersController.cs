using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions/plans/{planId:guid}/offers")]
public class SubscriptionOffersController(ISubscriptionOfferRepository subscriptionOfferRepository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SubscriptionOfferDto>>> GetAllOffers(Guid planId, CancellationToken ct)
    {
        var offers = await subscriptionOfferRepository.GetAllOffersAsync(planId, ct);
        return Ok(offers.Select(e => e.ToDto()));
    }

    [HttpGet("{offerId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<SubscriptionOfferDto>> GetOfferById(Guid planId, Guid offerId,
        CancellationToken ct)
    {
        var offer = await subscriptionOfferRepository.GetOfferById(offerId, ct);
        if (offer == null || offer.PlanId != planId)
            throw new NotFoundException("Предложение не найдено");
        return Ok(offer.ToDto());
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreateOffer(Guid planId, CreateSubscriptionOfferDto dto,
        CancellationToken ct)
    {
        var id = await subscriptionOfferRepository.CreateOfferAsync(planId, dto.Months, dto.Price, ct);
        return Ok(id);
    }

    [HttpPut("{offerId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdateOffer(Guid planId, Guid offerId, CreateSubscriptionOfferDto dto,
        CancellationToken ct)
    {
        var res = await subscriptionOfferRepository.UpdateOfferAsync(offerId, dto.Months, dto.Price, ct);
        if (!res)
            throw new NotFoundException("Предложение не найдено");
        return Ok();
    }

    [HttpDelete("{offerId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteOffer(Guid planId, Guid offerId, CancellationToken ct)
    {
        var res = await subscriptionOfferRepository.DeleteOfferAsync(offerId, ct);
        if (!res)
            throw new NotFoundException("Предложение не найдено");
        return Ok();
    }
}