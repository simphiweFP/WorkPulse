using WorkPulse.Domain.Entities;
using WorkPulse.Application.DTOs.Auth;

namespace WorkPulse.Application.Interfaces;

public interface ITokenService
{
    Task<TokenResultDto> GenerateTokenAsync(ApplicationUser user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
