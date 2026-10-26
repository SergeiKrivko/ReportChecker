using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Api.Extensions;
using ReportChecker.SourceProviders.GitHub;
using ReportChecker.SourceProviders.GitHub.Models;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/github")]
[Authorize]
public class GithubController(GithubService githubService) : ControllerBase
{
    [HttpGet("repositories")]
    public async Task<ActionResult<IEnumerable<RepositoryDto>>> GetRepositories()
    {
        var repositories = await githubService.GetRepositories(User.UserId);
        return Ok(repositories.Select(e => e.ToDto()));
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<string>>> GetBranches([FromQuery] long repositoryId)
    {
        return Ok(await githubService.GetBranchesOfRepositoryAsync(User.UserId, repositoryId));
    }

    [HttpGet("files")]
    public async Task<ActionResult<IEnumerable<RepositoryFileDto>>> GetFiles([FromQuery] long repositoryId,
        [FromQuery] string branch)
    {
        var files = await githubService.GetFilesOfRepositoryAsync(User.UserId, repositoryId, branch);
        return Ok(files.Select(e => e.ToDto()));
    }

    [HttpGet("installation")]
    public async Task<ActionResult<bool>> CheckInstallation()
    {
        return Ok(await githubService.CheckInstallation(User.UserId));
    }

    [HttpGet("repositories/{repositoryId:long}")]
    public async Task<ActionResult<RepositoryInfoDto>> GetRepository(long repositoryId)
    {
        var repository = await githubService.GetRepositoryInfoAsync(User.UserId, repositoryId);
        return Ok(repository.ToDto());
    }
}