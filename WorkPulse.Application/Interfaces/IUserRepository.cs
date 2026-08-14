using WorkPulse.Domain.Entities;

namespace WorkPulse.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task CreateAsync(ApplicationUser user, string passwordHash, IEnumerable<string> roles, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default);
}