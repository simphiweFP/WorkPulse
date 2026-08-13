using WorkPulse.Integration.Identity.Models;

namespace WorkPulse.Integration.Identity.Authentication;

public interface IJwtTokenService
{
    Task<string> CreateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
