using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Schemas;
using ReportChecker.Api.Utils;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/files")]
public class FilesController(IFileRepository fileRepository) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<UploadFileResponseSchema>> UploadFile(IFormFile file)
    {
        var user = User.Id;
        if (user == null)
            return Unauthorized();
        var id = Guid.NewGuid();
        await fileRepository.UploadFileAsync(FileRepositoryBucket.Sources, id, file.FileName,
            file.OpenReadStream());
        return Ok(new UploadFileResponseSchema
        {
            Id = id,
            FileName = file.FileName,
        });
    }
}