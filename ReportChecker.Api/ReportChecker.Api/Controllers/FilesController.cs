using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Models.Sources;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
public class FilesController(
    IFileRepository fileRepository,
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
}