using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Exceptions;
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
    IPatchService patchService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Comment>>> GetAllIssueComments(Guid reportId, Guid issueId)
    {
        var userId = User.UserId;

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (issue.CheckId != check?.Id)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId, userId);
        return Ok(comments);
    }

    [HttpGet("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Comment>> GetCommentById(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId);
        if (issue.CheckId != check?.Id)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var comment = await commentRepository.GetCommentByIdAsync(commentId, userId) ??
                      throw new NotFoundException($"Комментарий '{commentId}' не найден");
        return Ok(comment);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<Guid>> CreateIssueComment(Guid reportId, Guid issueId,
        [FromBody] CreateCommentSchema schema)
    {
        var userId = User.UserId;

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var id = await commentRepository.CreateCommentAsync(issueId, userId, schema.Content, schema.Status);
        if (!string.IsNullOrWhiteSpace(schema.Content))
            await checkService.WriteCommentAsync(report.Id, issueId);

        return Ok(id);
    }

    [HttpPut("{commentId:guid}")]
    [Authorize]
    public async Task<ActionResult<Guid>> UpdateIssueComment(Guid reportId, Guid issueId, Guid commentId,
        [FromBody] UpdateCommentSchema schema)
    {
        var userId = User.UserId;

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.UserId != userId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");

        await commentRepository.UpdateCommentAsync(commentId, schema.Content);
        return Ok(commentId);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<ActionResult> DeleteIssueComment(Guid reportId, Guid issueId, Guid commentId)
    {
        var userId = User.UserId;

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.UserId != userId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");

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
            throw new BadRequestException("На данный момент нельзя отметить комментарий как непрочитанный. " +
                                          "Поле `IsRead` должно быть `true`");
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
        if (report == null || report.OwnerId != userId)
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");

        var issue = await issueRepository.GetIssueByIdAsync(issueId);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var comment = await commentRepository.GetCommentByIdAsync(commentId);
        if (comment == null || comment.UserId != userId)
            throw new NotFoundException($"Комментарий '{commentId}' не найден либо написан другим пользователем");
        if (comment.Patch == null)
            throw new NotFoundException($"Комментарий '{commentId}' не содержит предложений по исправлению");

        await patchService.SetPatchStatus(comment.Patch.Id, schema.Status, ct);
        return Ok();
    }
}