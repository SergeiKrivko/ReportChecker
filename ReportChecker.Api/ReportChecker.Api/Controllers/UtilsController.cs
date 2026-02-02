using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class UtilsController(ReportCheckerDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Migrate()
    {
        await dbContext.Database.MigrateAsync();
        return Ok();
    }
}