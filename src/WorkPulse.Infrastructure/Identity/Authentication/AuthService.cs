using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.Common.Results;
using WorkPulse.Application.DTOs.Auth;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Constants;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Infrastructure.Identity.Authentication;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return Result<AuthResponseDto>.Failure("All registration fields are required.");
        }

        if (!request.Email.Contains('@'))
        {
            return Result<AuthResponseDto>.Failure("Invalid email address.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Result<AuthResponseDto>.Failure("Password and ConfirmPassword do not match.");
        }

        if (request.Password.Length < 8)
        {
            return Result<AuthResponseDto>.Failure("Password must be at least 8 characters long.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return Result<AuthResponseDto>.Failure("An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var roles = new[] { Roles.Employee };
        await _userRepository.CreateAsync(user, _passwordHasher.Hash(request.Password), roles, cancellationToken);

        var tokenResult = await _tokenService.GenerateTokenAsync(user, roles, cancellationToken);
        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAt = tokenResult.ExpiresAt,
            Roles = roles,
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles
            }
        });
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthResponseDto>.Failure("Email and password are required.");
        }

        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return Result<AuthResponseDto>.Failure("Invalid email or password.");
        }

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        var tokenResult = await _tokenService.GenerateTokenAsync(user, roles, cancellationToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAt = tokenResult.ExpiresAt,
            Roles = roles,
            User = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Roles = roles
            }
        });
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Missing user identity.");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Failure("User not found.");
        }

        var roles = await _userRepository.GetRolesAsync(user.Id, cancellationToken);
        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = roles
        });
    }
}
