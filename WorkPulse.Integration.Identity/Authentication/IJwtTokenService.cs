using WorkPulse.Integration.Identity.Models;

namespace WorkPulse.Integration.Identity.Authentication;

public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(AuthUser user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
