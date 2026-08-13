using Microsoft.AspNetCore.Identity;
using WorkPulse.Integration.Identity.Authentication;
using WorkPulse.Integration.Identity.Common;
using WorkPulse.Integration.Identity.Models;
using WorkPulse.Integration.Identity.Roles;

namespace WorkPulse.Integration.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public IdentityService(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result<AuthResponse>.Failure("Password and ConfirmPassword do not match.");
        }

        var existingUser = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (existingUser is not null)
        {
            return Result<AuthResponse>.Failure("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            UserName = request.Email.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Result<AuthResponse>.Failure(createResult.Errors.FirstOrDefault()?.Description ?? "Registration failed.");
        }

        await _userManager.AddToRoleAsync(user, WorkPulseRoles.Developer);

        var token = await _jwtTokenService.CreateTokenAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = await BuildUserAsync(user, cancellationToken),
            Token = token
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var token = await _jwtTokenService.CreateTokenAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = await BuildUserAsync(user, cancellationToken),
            Token = token
        });
    }

    public async Task<Result<AuthUser>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<AuthUser>.Failure("User not found.");
        }

        return Result<AuthUser>.Success(await BuildUserAsync(user, cancellationToken));
    }

    private async Task<AuthUser> BuildUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new AuthUser
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = role
        };
    }
}
