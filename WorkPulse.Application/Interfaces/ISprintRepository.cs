using WorkPulse.Domain.Entities;

namespace WorkPulse.Application.Interfaces;

public interface ISprintRepository
{
    Task<IReadOnlyCollection<Sprint>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
