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
    ICheckSourceRepository<FileCheckSource> checkSourceRepository,
    ICheckSourceRepository<LocalCheckSource> localCheckSourceRepository) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UploadFileResponseSchema>> UploadFile(IFormFile file,
        [FromQuery] FileBucketDto bucket = FileBucketDto.Default)
    {
        Guid id;
        switch (bucket)
        {
            case FileBucketDto.Default:
                id = await checkSourceRepository.CreateAsync(null, new FileCheckSource
                {
                    FileName = file.FileName,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null,
                });
                break;
            case FileBucketDto.Local:
                id = await localCheckSourceRepository.CreateAsync(null, new LocalCheckSource
                {
                    FileName = file.FileName,
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null,
                });
                break;
            default:
                return BadRequest("Unknown bucket");
        }

        if (!TryGetS3Buket(bucket, out var s3Bucket))
            return BadRequest("Unknown bucket");
        await fileRepository.UploadFileAsync(s3Bucket, id, file.FileName,
            file.OpenReadStream());
        return Ok(new UploadFileResponseSchema
        {
            Id = id,
            FileName = file.FileName,
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<DownloadUrlResponse>> DownloadFile([FromQuery(Name = "report")] Guid reportId,
        [FromQuery] FileBucketDto bucket = FileBucketDto.Default)
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
        if (!TryGetS3Buket(bucket, out var s3Bucket))
            return BadRequest("Unknown bucket");
        var fileUrl = source.Data.FileName == null
            ? await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id,
                TimeSpan.FromHours(1))
            : await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id, source.Data.FileName,
                TimeSpan.FromHours(1));
        return Ok(new DownloadUrlResponse
        {
            Url = fileUrl,
        });
    }

    private static bool TryGetS3Buket(FileBucketDto dto, out FileRepositoryBucket s3Bucket)
    {
        switch (dto)
        {
            case FileBucketDto.Default:
                s3Bucket = FileRepositoryBucket.Sources;
                return true;
            case FileBucketDto.Local:
                s3Bucket = FileRepositoryBucket.LocalSources;
                return true;
            default:
                s3Bucket = FileRepositoryBucket.Sources;
                return false;
        }
    }
}