using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/reports/{reportId:guid}/issues/{issueId:guid}/comments")]
public class CommentsController(
    IReportRepository reportRepository,
    IIssueRepository issueRepository,
    ICheckRepository checkRepository,
    ICheckService checkService,
    ICommentRepository commentRepository,
    ICommentReadRepository commentReadRepository,
    IPatchService patchService,
    ILimitsService limitsService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Comment>>> GetAllIssueComments(Guid reportId, Guid issueId)
    {
        var userId = User.UserId;

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

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId, userId);
        return Ok(comments);
    }

    [HttpGet("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Comment>> GetCommentById(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

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

        var comment = await commentRepository.GetCommentByIdAsync(commentId, userId);
        if (comment == null)
            return NotFound();
        return Ok(comment);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateIssueComment(Guid reportId, Guid issueId,
        [FromBody] CreateCommentSchema schema)
    {
        var userId = User.UserId;

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

        var id = await commentRepository.CreateCommentAsync(issueId, userId, schema.Content, schema.Status);

        if (await limitsService.CheckCommentsLimitAsync(userId, User.Subscriptions) && schema.Status == null)
            await checkService.WriteCommentAsync(check.Id, issueId);
        return Ok(id);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Guid>> UpdateIssueComment(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdateCommentSchema schema)
    {
        var userId = User.UserId;

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
            return NotFound();
        if (comment.UserId != userId)
            return Unauthorized();

        await commentRepository.UpdateCommentAsync(commentId, schema.Content);
        return Ok(commentId);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<ActionResult> DeleteIssueComment(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
            return NotFound();
        if (comment.UserId != userId)
            return Unauthorized();

        await commentRepository.DeleteCommentAsync(commentId);
        return Ok();
    }

    [HttpPost("read")]
    public async Task<ActionResult> MarkRead(Guid reportId, Guid issueId, [FromBody] MarkReadSchema schema,
        CancellationToken ct)
    {
        var userId = User.UserId;

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId, userId);
        var commentIds = comments.Where(e => e.IsRead != schema.IsRead).Select(e => e.Id);
        if (schema.CommentIds.Length > 0)
            commentIds = commentIds.Where(e => schema.CommentIds.Contains(e));
        if (schema.IsRead)
        {
            await commentReadRepository.AddAsync(userId, commentIds, ct);
        }
        else
        {
            return BadRequest("Not implemented");
        }

        return Ok();
    }

    [HttpPut("{commentId:guid}/patch")]
    [Authorize]
    public async Task<ActionResult> UpdatePatchStatus(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdatePatchSchema schema, CancellationToken ct = default)
    {
        var userId = User.UserId;

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            return NotFound();
        if (report.OwnerId != userId)
            return Unauthorized();

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            return NotFound();

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null)
            return NotFound();
        if (comment.IssueId != issueId)
            return Unauthorized();
        if (comment.Patch == null)
            return NotFound();

        await patchService.SetPatchStatus(comment.Patch.Id, schema.Status, ct);
        return Ok();
    }
}