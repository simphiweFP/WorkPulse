using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Constants;
using WorkPulse.Web.API.Contracts.Requests.Users;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UsersController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        => Ok(await _userRepository.GetUserManagementAsync(cancellationToken));

    [HttpPatch("{userId}/role")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateRole(string userId, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (string.Equals(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, userId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "You cannot change your own role." });
        }

        var normalizedRole = request.Role.Trim();
        if (!string.Equals(normalizedRole, Roles.Pending, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, Roles.Developer, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalizedRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Invalid role value." });
        }

        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var currentRoles = await _userRepository.GetRolesAsync(userId, cancellationToken);
        var isCurrentAdmin = currentRoles.Any(role => string.Equals(role, Roles.Admin, StringComparison.OrdinalIgnoreCase));
        if (isCurrentAdmin && !string.Equals(normalizedRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                return BadRequest(new { message = "The final administrator account cannot be removed or demoted." });
            }
        }

        await _userRepository.UpdateRoleAsync(userId, normalizedRole, cancellationToken);
        return NoContent();
    }
}
