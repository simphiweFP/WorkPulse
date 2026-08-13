using WorkPulse.Application.Common.Results;
using WorkPulse.Application.DTOs.Auth;

namespace WorkPulse.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);
}
