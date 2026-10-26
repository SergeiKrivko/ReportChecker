using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController(ILlmModelRepository llmModelRepository, IAiHelperService aiHelperService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LlmModelDto>>> GetAllModelsAsync(bool source = false,
        CancellationToken ct = default)
    {
        var models = await llmModelRepository.GetAllModelsAsync(ct);
        if (source)
            return Ok(models.Select(e => e.ToDto()));
        var pricing = await aiHelperService.GetModelsPricingAsync(ct);
        return Ok(models.Join(pricing, e => e.ModelKey, e => e.ModelId, (model, price) => model.ToDto(price)));
    }

    [HttpGet("{modelId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<LlmModelDto>> GetModelByIdAsync(Guid modelId,
        CancellationToken ct = default)
    {
        var model = await llmModelRepository.GetModelByIdAsync(modelId, ct) ??
                    throw new NotFoundException($"Модель '{modelId}' не найдена");
        var pricing = await aiHelperService.GetModelsPricingAsync(ct);
        return Ok(model.ToDto(pricing.First(e => e.ModelId == model.ModelKey)));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreateModelAsync(CreateLlmModelDto dto, CancellationToken ct = default)
    {
        var id = await llmModelRepository.CreateModelAsync(dto.DisplayName, dto.ModelKey,
            dto.InputCoefficient, dto.OutputCoefficient, dto.DefaultReasoningLevel, dto.IsRecommended, ct);
        return Ok(id);
    }

    [HttpPut("{modelId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdateModelAsync(Guid modelId, CreateLlmModelDto dto,
        CancellationToken ct = default)
    {
        var res = await llmModelRepository.UpdateModelAsync(modelId, dto.DisplayName, dto.ModelKey,
            dto.InputCoefficient, dto.OutputCoefficient, dto.DefaultReasoningLevel, dto.IsRecommended, ct);
        if (!res)
            throw new NotFoundException($"Модель '{modelId}' не найдена");
        return Ok();
    }

    [HttpDelete("{modelId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteModelByIdAsync(Guid modelId,
        CancellationToken ct = default)
    {
        var res = await llmModelRepository.DeleteModelAsync(modelId, ct);
        if (!res)
            throw new NotFoundException($"Модель '{modelId}' не найдена");
        return Ok();
    }
}