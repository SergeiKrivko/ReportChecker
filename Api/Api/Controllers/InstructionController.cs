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
[Route("api/v1/reports/{reportId:guid}/instructions")]
public class InstructionController(
    IInstructionRepository instructionRepository,
    IReportAccessService reportAccessService,
    IInstructionTaskRepository instructionTaskRepository,
    IInstructionTaskService instructionTaskService)
    : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InstructionDto>>> GetInstructionsAsync(Guid reportId,
        CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Read, ct);
        var instructions = await instructionRepository.GetInstructionsAsync(reportId, ct);
        return Ok(instructions.Select(e => e.ToDto()));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateInstruction(Guid reportId, [FromBody] string instruction,
        CancellationToken ct)
    {
        var userId = User.UserId;
        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Edit, ct);
        var id = await instructionRepository.CreateInstructionAsync(reportId, instruction, userId, null, ct);
        return Ok(id);
    }

    [HttpPut("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateInstructionAsync(Guid reportId, Guid instructionId, [FromBody] string content,
        CancellationToken ct)
    {
        var userId = User.UserId;
        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Edit, ct);
        var instruction = await instructionRepository.GetInstructionByIdAsync(instructionId, ct);
        if (instruction == null || instruction.ReportId != reportId)
            throw new NotFoundException($"Инструкция '{instructionId}' не найдена");
        var id = await instructionRepository.UpdateInstructionAsync(instructionId, content, userId, ct);
        return Ok(id);
    }

    [HttpDelete("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteInstructionAsync(Guid reportId, Guid instructionId,
        CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit, ct);
        var instruction = await instructionRepository.GetInstructionByIdAsync(instructionId, ct);
        if (instruction == null || instruction.ReportId != reportId)
            throw new NotFoundException($"Инструкция '{instructionId}' не найдена");
        var id = await instructionRepository.DeleteInstructionAsync(instructionId, ct);
        return Ok(id);
    }

    [HttpGet("tasks")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InstructionTaskDto>>> GetInstructionTasks(Guid reportId)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Read);
        var tasks = await instructionTaskRepository.GetAllForReportAsync(reportId, ProgressStatus.InProgress);
        return Ok(tasks.Select(e => e.ToDto()));
    }

    [HttpPost("tasks")]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateInstructionTaskAsync(Guid reportId,
        [FromBody] CreateInstructionTaskDto dto, CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit, ct);

        if (dto.InstructionId != null)
        {
            var id = await instructionTaskService.CreateInstructionTaskAsync(reportId, dto.InstructionId.Value,
                dto.Mode,
                ct);
            return Ok(id);
        }

        if (dto.Instruction != null)
        {
            var id = await instructionTaskService.CreateInstructionTaskAsync(reportId, dto.Instruction, dto.Mode,
                ct);
            return Ok(id);
        }

        throw new BadRequestException("Both InstructionId and Instruction fields are empty");
    }

    [HttpDelete("tasks/{taskId:guid}")]
    [Authorize]
    public async Task<ActionResult> CancelInstructionTask(Guid reportId, Guid taskId, CancellationToken ct)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit, ct);
        var task = await instructionTaskRepository.GetByIdAsync(taskId, ct);
        if (task == null || task.ReportId != reportId)
            throw new NotFoundException($"Задача '{taskId}' не найдена");

        await instructionTaskService.CancelInstructionTaskAsync(taskId, ct);
        return Ok();
    }
}
