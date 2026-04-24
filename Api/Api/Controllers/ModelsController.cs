using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/models")]
public class ModelsController(ILlmModelRepository llmModelRepository) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<LlmModel>>> GetAllModelsAsync(CancellationToken ct = default)
    {
        var models = await llmModelRepository.GetAllModelsAsync(ct);
        return Ok(models);
    }

    [HttpGet("{modelId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<LlmModel>> GetModelByIdAsync(Guid modelId,
        CancellationToken ct = default)
    {
        var model = await llmModelRepository.GetModelByIdAsync(modelId, ct);
        if (model == null)
            return NotFound();
        return Ok(model);
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<Guid>> CreateModelAsync(CreateLlmModelSchema schema, CancellationToken ct = default)
    {
        var id = await llmModelRepository.CreateModelAsync(schema.DisplayName, schema.ModelKey, ct);
        return Ok(id);
    }

    [HttpDelete("{modelId:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteModelByIdAsync(Guid modelId,
        CancellationToken ct = default)
    {
        var res = await llmModelRepository.DeleteModelAsync(modelId, ct);
        if (!res)
            return NotFound();
        return Ok();
    }
}