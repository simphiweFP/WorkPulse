using System.Collections.Concurrent;
using WorkPulse.Integration.Identity.Common;
using WorkPulse.Integration.Identity.Models;
using WorkPulse.Integration.Identity.Roles;
using WorkPulse.Integration.Identity.Services;

namespace WorkPulse.Web.API.Tests.Infrastructure;

internal sealed class FakeIdentityService : IIdentityService
{
    private readonly ConcurrentDictionary<string, FakeUser> _users = new(StringComparer.OrdinalIgnoreCase);
    public FakeIdentityService()
    {
        var admin = new FakeUser
        {
            Id = "test-admin",
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@workpulse.local",
            Password = "WorkPulseAdmin123!",
            Role = WorkPulseRoles.Admin
        };

        _users[admin.Email] = admin;
    }
    public Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return Task.FromResult(Result<AuthResponse>.Failure("Password and ConfirmPassword do not match."));
        }

        if (request.Password.Length < 8)
        {
            return Task.FromResult(Result<AuthResponse>.Failure("Password must be at least 8 characters long."));
        }

        var email = request.Email.Trim();
        if (_users.ContainsKey(email))
        {
            return Task.FromResult(Result<AuthResponse>.Failure("An account with this email already exists."));
        }

        var user = new FakeUser
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Password = request.Password,
            Role = WorkPulseRoles.Pending
        };

        _users[email] = user;
        return Task.FromResult(Result<AuthResponse>.Success(BuildAuthResponse(user)));
    }

    public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_users.TryGetValue(request.Email.Trim(), out var user) || !string.Equals(user.Password, request.Password, StringComparison.Ordinal))
        {
            return Task.FromResult(Result<AuthResponse>.Failure("Invalid email or password."));
        }

        return Task.FromResult(Result<AuthResponse>.Success(BuildAuthResponse(user)));
    }

    public Task<Result<AuthUser>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = _users.Values.FirstOrDefault(item => string.Equals(item.Id, userId, StringComparison.Ordinal));
        return user is null
            ? Task.FromResult(Result<AuthUser>.Failure("User not found."))
            : Task.FromResult(Result<AuthUser>.Success(new AuthUser
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role
            }));
    }

    private static AuthResponse BuildAuthResponse(FakeUser user) => new()
    {
        User = new AuthUser
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role
        },
        Token = $"fake-token|{user.Role}|{user.Id}"
    };

    private sealed class FakeUser
    {
        public string Id { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
    }
}