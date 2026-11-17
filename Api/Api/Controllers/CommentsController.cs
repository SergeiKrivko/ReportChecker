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
[Route("api/v1/reports/{reportId:guid}/issues/{issueId:guid}/comments")]
public class CommentsController(
    IReportAccessService reportAccessService,
    IIssueRepository issueRepository,
    ICheckRepository checkRepository,
    ICheckService checkService,
    ICommentRepository commentRepository,
    ICommentReadRepository commentReadRepository,
    IPatchService patchService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetAllIssueComments(Guid reportId, Guid issueId)
    {
        var userId = User.UserId;

        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Read);
        await GetIssueAsync(reportId, issueId);

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId, userId);
        return Ok(comments.Select(e => e.ToDto()));
    }

    [HttpGet("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> GetCommentById(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Read);
        await GetIssueAsync(reportId, issueId);

        var comment = await commentRepository.GetCommentByIdAsync(commentId, userId);
        if (comment == null || comment.IssueId != issueId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден");
        return Ok(comment.ToDto());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateIssueComment(Guid reportId, Guid issueId,
        [FromBody] CreateCommentDto dto)
    {
        var userId = User.UserId;

        var report = (await reportAccessService.RequireAccessAsync(reportId, userId,
            ReportAccessLevel.Comment)).Report;
        await GetIssueAsync(reportId, issueId);

        var id = await commentRepository.CreateCommentAsync(issueId, userId, dto.Content, dto.Status);
        if (!string.IsNullOrWhiteSpace(dto.Content))
            await checkService.WriteCommentAsync(report.Id, issueId);

        return Ok(id);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Guid>> UpdateIssueComment(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdateCommentDto dto)
    {
        var userId = User.UserId;

        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Comment);
        await GetIssueAsync(reportId, issueId);

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.IssueId != issueId || comment.UserId != userId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");

        await commentRepository.UpdateCommentAsync(commentId, dto.Content);
        return Ok(commentId);
    }

    [HttpDelete("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult> DeleteIssueComment(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Comment);
        await GetIssueAsync(reportId, issueId);

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.IssueId != issueId || comment.UserId != userId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");

        await commentRepository.DeleteCommentAsync(commentId);
        return Ok();
    }

    [HttpPost("read")]
    [Authorize]
    public async Task<ActionResult> MarkRead(Guid reportId, Guid issueId, [FromBody] MarkReadDto dto,
        CancellationToken ct)
    {
        var userId = User.UserId;

        await reportAccessService.RequireAccessAsync(reportId, userId, ReportAccessLevel.Read);
        await GetIssueAsync(reportId, issueId, ct);

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId, userId);
        var commentIds = comments.Where(e => e.IsRead != dto.IsRead).Select(e => e.Id);
        if (dto.CommentIds.Length > 0)
            commentIds = commentIds.Where(e => dto.CommentIds.Contains(e));
        if (dto.IsRead)
        {
            await commentReadRepository.AddAsync(userId, commentIds, ct);
        }
        else
        {
            await commentReadRepository.DeleteAsync(userId, commentIds, ct);
        }

        return Ok();
    }

    [HttpPut("{commentId:guid}/patch")]
    [Authorize]
    public async Task<ActionResult> UpdatePatchStatus(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdatePatchDto dto, CancellationToken ct = default)
    {
        await reportAccessService.RequireAccessAsync(reportId, User.UserId, ReportAccessLevel.Edit);
        await GetIssueAsync(reportId, issueId, ct);

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.IssueId != issueId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");
        if (comment.Patch == null)
            throw new NotFoundException($"Комментарий '{commentId}' не содержит предложений по исправлению");

        await patchService.SetPatchStatus(comment.Patch.Id, dto.Status, ct);
        return Ok();
    }

    private async Task<Issue> GetIssueAsync(Guid reportId, Guid issueId, CancellationToken ct = default)
    {
        var issue = await issueRepository.GetIssueByIdAsync(issueId, ct);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId, ct);
        if (check?.ReportId != reportId)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        return issue;
    }
}
