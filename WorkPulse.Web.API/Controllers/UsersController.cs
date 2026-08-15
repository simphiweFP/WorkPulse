using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet("developers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDevelopers(CancellationToken cancellationToken)
        => Ok(await _userRepository.GetDevelopersAsync(cancellationToken));
}
