using WorkPulse.Integration.Identity.Common;
using WorkPulse.Integration.Identity.Models;

namespace WorkPulse.Integration.Identity.Services;

public interface IIdentityService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthUser>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
}
