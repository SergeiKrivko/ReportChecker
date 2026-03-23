using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<ActionResult<IEnumerable<Repository>>> GetRepositories()
    {
        return Ok(await githubService.GetRepositories(User.UserId));
    }

    [HttpGet("branches")]
    public async Task<ActionResult<IEnumerable<string>>> GetBranches([FromQuery] long repositoryId)
    {
        return Ok(await githubService.GetBranchesOfRepositoryAsync(User.UserId, repositoryId));
    }

    [HttpGet("files")]
    public async Task<ActionResult<IEnumerable<RepositoryFile>>> GetFiles([FromQuery] long repositoryId,
        [FromQuery] string branch)
    {
        return Ok(await githubService.GetFilesOfRepositoryAsync(User.UserId, repositoryId, branch));
    }

    [HttpGet("installation")]
    public async Task<ActionResult<bool>> CheckInstallation()
    {
        return Ok(await githubService.CheckInstallation(User.UserId));
    }

    [HttpGet("repositories/{repositoryId:long}")]
    public async Task<ActionResult<RepositoryInfo>> GetRepository(long repositoryId)
    {
        return Ok(await githubService.GetRepositoryInfoAsync(User.UserId, repositoryId));
    }
}