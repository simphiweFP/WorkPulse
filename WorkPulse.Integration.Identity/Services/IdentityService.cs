using WorkPulse.Application.Interfaces;
using WorkPulse.Integration.Identity.Authentication;
using WorkPulse.Integration.Identity.Common;
using WorkPulse.Integration.Identity.Models;
using WorkPulse.Integration.Identity.Roles;
using DomainUser = WorkPulse.Domain.Entities.ApplicationUser;
using PasswordHasherContract = WorkPulse.Application.Interfaces.IPasswordHasher;

namespace WorkPulse.Integration.Identity.Services;

public class IdentityService : IIdentityService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasherContract _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public IdentityService(IUserRepository userRepository, PasswordHasherContract passwordHasher, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Result<AuthResponse>.Failure("Password and ConfirmPassword do not match.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return Result<AuthResponse>.Failure("An account with this email already exists.");
        }

        var user = new DomainUser
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            UserName = email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var roles = new[] { WorkPulseRoles.Developer };
        await _userRepository.CreateAsync(user, _passwordHasher.Hash(request.Password), roles, cancellationToken);

        var authUser = await BuildUserAsync(user, roles, cancellationToken);
        var token = await _jwtTokenService.CreateTokenAsync(authUser, roles, cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = authUser,
            Token = token
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        var authUser = await BuildUserAsync(user, roles, cancellationToken);
        var token = await _jwtTokenService.CreateTokenAsync(authUser, roles, cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse
        {
            User = authUser,
            Token = token
        });
    }

    public async Task<Result<AuthUser>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AuthUser>.Failure("User not found.");
        }

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        return Result<AuthUser>.Success(await BuildUserAsync(user, roles, cancellationToken));
    }

    private static Task<AuthUser> BuildUserAsync(DomainUser user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new AuthUser
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }
}
