using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Extensions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
public class FilesController(
    IFileRepository fileRepository,
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    ICheckSourceRepository<FileCheckSource> checkSourceRepository) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UploadFileResponseSchema>> UploadFile(IFormFile file)
    {
        var id = await checkSourceRepository.CreateAsync(null, new FileCheckSource
        {
            FileName = file.FileName,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = null,
        });
        await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, file.FileName,
            file.OpenReadStream());
        return Ok(new UploadFileResponseSchema
        {
            Id = id,
            FileName = file.FileName,
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<DownloadUrlResponse>> DownloadFile([FromQuery(Name = "report")] Guid reportId)
    {
        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (User.UserId != report?.OwnerId)
            return Unauthorized();
        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId);
        if (check == null)
            return NotFound();
        var source = await checkSourceRepository.GetByCheckIdAsync(check.Id);
        if (source == null)
            return NotFound();
        var fileUrl = source.Data.FileName == null
            ? await fileRepository.GetDownloadUrlAsync(FileRepositoryBucket.Sources, source.Id,
                TimeSpan.FromHours(1))
            : await fileRepository.GetDownloadUrlAsync(FileRepositoryBucket.Sources, source.Id, source.Data.FileName,
                TimeSpan.FromHours(1));
        return Ok(new DownloadUrlResponse
        {
            Url = fileUrl,
        });
    }
}