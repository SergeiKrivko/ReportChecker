using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportChecker.Abstractions;
using ReportChecker.Api.Converters;
using ReportChecker.Api.Dtos;
using ReportChecker.Exceptions;

namespace ReportChecker.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetUserByIdAsync(userId, ct) ??
                   throw new NotFoundException($"Пользователь '{userId}' не найден");
        return Ok(user.ToPublicDto());
    }
}
