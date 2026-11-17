using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.Exceptions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
public class FilesController(
    IFileRepository fileRepository,
    IReportAccessService reportAccessService,
    ICheckRepository checkRepository,
    ICheckSourceRepository<FileCheckSource> checkSourceRepository,
    ICheckSourceRepository<LocalCheckSource> localCheckSourceRepository) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UploadFileResponseDto>> UploadFile(IFormFile file,
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
                throw new BadRequestException("Unknown bucket");
        }

        if (!bucket.TryGetS3Bucket(out var s3Bucket))
            throw new BadRequestException("Unknown bucket");
        await fileRepository.UploadFileAsync(s3Bucket, id, file.FileName,
            file.OpenReadStream());
        return Ok(new UploadFileResponseDto
        {
            Id = id,
            FileName = file.FileName,
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<DownloadUrlDto>> DownloadFile([FromQuery(Name = "report")] Guid reportId,
        [FromQuery] FileBucketDto bucket = FileBucketDto.Default)
    {
        var access = await reportAccessService.RequireAccessAsync(reportId, User.UserId,
            ReportAccessLevel.Read);
        var check = await checkRepository.GetLatestCheckOfReportAsync(access.Report.Id);
        if (check == null)
            throw new NotFoundException($"Отчет '{reportId}' не содержит ни одной проверки");
        var source = await checkSourceRepository.GetByCheckIdAsync(check.Id);
        if (source == null)
            throw new NotFoundException($"Не удалось получить информацию об источнике файла");
        if (!bucket.TryGetS3Bucket(out var s3Bucket))
            throw new BadRequestException("Unknown bucket");
        var fileUrl = source.Data.FileName == null
            ? await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id,
                TimeSpan.FromHours(1))
            : await fileRepository.GetDownloadUrlAsync(s3Bucket, source.Id, source.Data.FileName,
                TimeSpan.FromHours(1));
        return Ok(new DownloadUrlDto
        {
            Url = fileUrl,
        });
    }
}
