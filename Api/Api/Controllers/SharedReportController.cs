using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/shared/reports/{token}")]
[AllowAnonymous]
public class SharedReportController(
    IReportAccessService reportAccessService,
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    IIssueRepository issueRepository,
    ICommentRepository commentRepository,
    IInstructionRepository instructionRepository,
    IInstructionTaskRepository instructionTaskRepository,
    ICheckService checkService,
    IFileRepository fileRepository,
    ICheckSourceRepository<FileCheckSource> checkSourceRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportDto>> GetReportAsync(string token, CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var detailed = await reportRepository.GetReportDetailsAsync(report.Id, ct) ??
                       throw new NotFoundException("Ссылка недействительна либо отозвана");
        return Ok(detailed.ToDto(ReportAccessLevel.Read));
    }

    [HttpGet("checks")]
    public async Task<ActionResult<IEnumerable<CheckDto>>> GetChecksAsync(string token, CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var checks = await checkRepository.GetAllChecksOfReportAsync(report.Id, ct);
        return Ok(checks.Select(e => e.ToDto()));
    }

    [HttpGet("checks/latest")]
    public async Task<ActionResult<CheckDto>> GetLatestCheckAsync(string token, CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var check = await checkRepository.GetLatestCheckOfReportAsync(report.Id, true, ct) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");
        return Ok(check.ToDto());
    }

    [HttpGet("checks/latest/chapters")]
    public async Task<ActionResult<IEnumerable<ChapterDto>>> GetLatestCheckChaptersAsync(string token,
        CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var check = await checkRepository.GetLatestCheckOfReportAsync(report.Id, ct: ct) ??
                    throw new NotFoundException("Не найдено ни одной проверки для данного отчета");
        var chapters = await checkService.GetChaptersAsync(report, check);
        return Ok(chapters.Select(e => e.ToDto()));
    }

    [HttpGet("issues")]
    public async Task<ActionResult<IEnumerable<IssueDto>>> GetIssuesAsync(string token, CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var issues = await issueRepository.GetAllIssuesOfReportAsync(report.Id, ct);
        return Ok(issues.Select(e => e.ToDto()));
    }

    [HttpGet("issues/{issueId:guid}")]
    public async Task<ActionResult<IssueDto>> GetIssueAsync(string token, Guid issueId, CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var issue = await GetIssueAsync(report, issueId, ct);
        return Ok(issue.ToDto());
    }

    [HttpGet("issues/{issueId:guid}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetIssueCommentsAsync(string token, Guid issueId,
        CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        await GetIssueAsync(report, issueId, ct);
        var comments = await commentRepository.GetAllCommentsOfIssueAsync(issueId);
        return Ok(comments.Select(e => e.ToDto()));
    }

    [HttpGet("instructions")]
    public async Task<ActionResult<IEnumerable<InstructionDto>>> GetInstructionsAsync(string token,
        CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var instructions = await instructionRepository.GetInstructionsAsync(report.Id, ct);
        return Ok(instructions.Select(e => e.ToDto()));
    }

    [HttpGet("instructions/tasks")]
    public async Task<ActionResult<IEnumerable<InstructionTaskDto>>> GetInstructionTasksAsync(string token,
        CancellationToken ct)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var tasks = await instructionTaskRepository.GetAllForReportAsync(report.Id, ProgressStatus.InProgress);
        return Ok(tasks.Select(e => e.ToDto()));
    }

    [HttpGet("file")]
    public async Task<ActionResult<DownloadUrlDto>> DownloadFileAsync(string token,
        [FromQuery] FileBucketDto bucket = FileBucketDto.Default, CancellationToken ct = default)
    {
        var report = await reportAccessService.RequireSharedReportAsync(token, ct);
        var check = await checkRepository.GetLatestCheckOfReportAsync(report.Id, ct: ct);
        if (check == null)
            throw new NotFoundException($"Отчет '{report.Id}' не содержит ни одной проверки");
        var source = await checkSourceRepository.GetByCheckIdAsync(check.Id);
        if (source == null)
            throw new NotFoundException("Не удалось получить информацию об источнике файла");
        if (!bucket.TryGetS3Bucket(out var s3Bucket))
            throw new BadRequestException("Unknown bucket");
        var fileUrl = source.Data.FileName == null
            ? await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id, TimeSpan.FromHours(1))
            : await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id, source.Data.FileName,
                TimeSpan.FromHours(1));
        return Ok(new DownloadUrlDto
        {
            Url = fileUrl,
        });
    }

    private async Task<Issue> GetIssueAsync(Report report, Guid issueId, CancellationToken ct)
    {
        var issue = await issueRepository.GetIssueByIdAsync(issueId, ct);
        if (issue == null)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        var check = await checkRepository.GetCheckByIdAsync(issue.CheckId, ct);
        if (check?.ReportId != report.Id)
            throw new NotFoundException($"Ошибка '{issueId}' не найдена");

        return issue;
    }
}
