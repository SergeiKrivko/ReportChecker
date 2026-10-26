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
    IReportRepository reportRepository,
    IInstructionTaskRepository instructionTaskRepository,
    IInstructionTaskService instructionTaskService)
    : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InstructionDto>>> GetInstructionsAsync(Guid reportId,
        CancellationToken ct)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var instructions = await instructionRepository.GetInstructionsAsync(reportId, ct);
        return Ok(instructions.Select(e => e.ToDto()));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateInstruction(Guid reportId, [FromBody] string instruction,
        CancellationToken ct)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var id = await instructionRepository.CreateInstructionAsync(reportId, instruction, userId, null, ct);
        return Ok(id);
    }

    [HttpPut("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> UpdateInstructionAsync(Guid reportId, Guid instructionId, [FromBody] string content,
        CancellationToken ct)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var id = await instructionRepository.UpdateInstructionAsync(instructionId, content, userId, ct);
        return Ok(id);
    }

    [HttpDelete("{instructionId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteInstructionAsync(Guid reportId, Guid instructionId,
        CancellationToken ct)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var id = await instructionRepository.DeleteInstructionAsync(instructionId, ct);
        return Ok(id);
    }

    [HttpGet("tasks")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InstructionTaskDto>>> GetInstructionTasks(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var tasks = await instructionTaskRepository.GetAllForReportAsync(reportId, ProgressStatus.InProgress);
        return Ok(tasks.Select(e => e.ToDto()));
    }

    [HttpPost("tasks")]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateInstructionTaskAsync(Guid reportId,
        [FromBody] CreateInstructionTaskDto dto, CancellationToken ct)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != User.UserId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

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
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != User.UserId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var task = await instructionTaskRepository.GetByIdAsync(taskId, ct);
        if (task == null || task.ReportId != reportId)
            throw new NotFoundException($"Задача '{taskId}' не найдена");

        await instructionTaskService.CancelInstructionTaskAsync(taskId, ct);
        return Ok();
    }
}