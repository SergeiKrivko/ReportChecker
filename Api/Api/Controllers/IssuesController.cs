using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/issues")]
public class IssuesController(
    IReportRepository reportRepository,
    IIssueRepository issueRepository,
    ICheckRepository checkRepository) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Issue>>> GetAllIssues(Guid reportId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        var result = await issueRepository.GetAllIssuesOfReportAsync(reportId, userId);
        return Ok(result);
    }

    [HttpGet("{issueId:guid}")]
    [Authorize]
    public async Task<ActionResult<Issue>> GetIssueById(Guid reportId, Guid issueId)
    {
        var userId = User.UserId;
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var issue = await issueRepository.GetIssueByIdAsync(issueId, userId);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (check?.ReportId != report.Id)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        return Ok(issue);
    }
}