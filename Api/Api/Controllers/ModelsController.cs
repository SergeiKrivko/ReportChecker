using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController(ILlmModelRepository llmModelRepository, IAiHelperService aiHelperService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LlmModel>>> GetAllModelsAsync(bool source = false, CancellationToken ct = default)
    {
        var models = await llmModelRepository.GetAllModelsAsync(ct);
        if (source)
            return Ok(models);
        var pricing = await aiHelperService.GetModelsPricingAsync(ct);
        return Ok(models.Join(pricing, e => e.ModelKey, e => e.ModelId, AddPrice));
    }

    [HttpGet("{modelId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<LlmModel>> GetModelByIdAsync(Guid modelId,
        CancellationToken ct = default)
    {
        var model = await llmModelRepository.GetModelByIdAsync(modelId, ct) ??
                    throw new NotFoundException($"Модель '{modelId}' не найдена");
        var pricing = await aiHelperService.GetModelsPricingAsync(ct);
        return Ok(AddPrice(model, pricing.First(e => e.ModelId == model.ModelKey)));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreateModelAsync(CreateLlmModelSchema schema, CancellationToken ct = default)
    {
        var id = await llmModelRepository.CreateModelAsync(schema.DisplayName, schema.ModelKey,
            schema.InputCoefficient, schema.OutputCoefficient, ct);
        return Ok(id);
    }

    [HttpPut("{modelId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> UpdateModelAsync(Guid modelId, CreateLlmModelSchema schema,
        CancellationToken ct = default)
    {
        var res = await llmModelRepository.UpdateModelAsync(modelId, schema.DisplayName, schema.ModelKey,
            schema.InputCoefficient, schema.OutputCoefficient, ct);
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

    private static LlmModel AddPrice(LlmModel m, LLmModelPrice p)
    {
        return new LlmModel
        {
            Id = m.Id,
            DisplayName = m.DisplayName,
            ModelKey = m.ModelKey,
            CreatedAt = m.CreatedAt,
            DeletedAt = m.DeletedAt,
            IsDefault = m.IsDefault,
            InputCoefficient = (double)p.InputCoefficient,
            OutputCoefficient = (double)p.OutputCoefficient,
        };
    }
}