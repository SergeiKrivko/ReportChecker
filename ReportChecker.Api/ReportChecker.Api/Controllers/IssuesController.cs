using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/issues")]
public class IssuesController(
    IAuthService authService,
    IReportRepository reportRepository,
    IIssueRepository issueRepository,
    ICheckRepository checkRepository,
    ICheckService checkService,
    ICommentRepository commentRepository) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Issue>>> GetAllIssues(Guid reportId)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();
        var result = await issueRepository.GetAllIssuesOfReportAsync(reportId);
        return Ok(result);
    }

    [HttpGet("{issueId:guid}")]
    [Authorize]
    public async Task<ActionResult<Issue>> GetIssueById(Guid reportId, Guid issueId)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            return NotFound();

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (issue.CheckId != check?.Id)
            return NotFound();

        return Ok(issue);
    }

    [HttpGet("{issueId:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Comment>>> GetAllIssueComments(Guid reportId, Guid issueId)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            return NotFound();

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (issue.CheckId != check?.Id)
            return NotFound();

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId);
        return Ok(comments);
    }

    [HttpPost("{issueId:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateIssueComment(Guid reportId, Guid issueId,
        [FromBody] CreateCommentSchema schema)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            return NotFound();

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (issue.CheckId != check?.Id)
            return NotFound();

        var id = await commentRepository.CreateCommentAsync(issueId, userId.Value, schema.Content, schema.Status);
        if (schema.Status == null)
            await checkService.WriteCommentAsync(check.Id, issueId);
        return Ok(id);
    }

    [HttpPut("{issueId:guid}/comments/{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Guid>> UpdateIssueComment(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdateCommentSchema schema)
    {
        var userId = await authService.AuthenticateAsync(User);
        if (userId == null)
            return Unauthorized();

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
            return NotFound();
        if (comment.UserId != userId)
            return Unauthorized();

        await commentRepository.UpdateCommentAsync(commentId, schema.Content);
        return Ok(commentId);
    }
}