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
}