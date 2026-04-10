using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/instructions")]
public class InstructionController(
    IInstructionRepository instructionRepository,
    IReportRepository reportRepository,
    IInstructionTaskRepository instructionTaskRepository,
    IInstructionTaskService instructionTaskService)
    : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Instruction>>> GetInstructionsAsync(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var instructions = await instructionRepository.GetInstructionsAsync(reportId);
        return Ok(instructions);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateInstruction(Guid reportId, [FromBody] string instruction)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var id = await instructionRepository.CreateInstructionAsync(reportId, instruction);
        return Ok(id);
    }

    [HttpPut("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateInstructionAsync(Guid reportId, Guid instructionId, [FromBody] string content)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var id = await instructionRepository.UpdateInstructionAsync(instructionId, content);
        return Ok(id);
    }

    [HttpDelete("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteInstructionAsync(Guid reportId, Guid instructionId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var id = await instructionRepository.DeleteInstructionAsync(instructionId);
        return Ok(id);
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<IEnumerable<InstructionTask>>> GetInstructionTasks(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var tasks = await instructionTaskRepository.GetAllForReportAsync(reportId, ProgressStatus.InProgress);
        return Ok(tasks);
    }

    [HttpPost("tasks")]
    public async Task<ActionResult<Guid>> CreateInstructionTaskAsync(Guid reportId,
        [FromBody] CreateInstructionTaskSchema schema, CancellationToken ct)
    {
        if (schema.InstructionId != null)
        {
            var id = await instructionTaskService.CreateInstructionTaskAsync(reportId, schema.InstructionId.Value, schema.Mode,
                ct);
            return Ok(id);
        }
        if (schema.Instruction != null)
        {
            var id = await instructionTaskService.CreateInstructionTaskAsync(reportId, schema.Instruction, schema.Mode,
                ct);
            return Ok(id);
        }
        return BadRequest("Both InstructionId and Instruction fields are empty");
    }
}