using WorkPulse.Domain.Entities;

namespace WorkPulse.Application.Interfaces;

public interface IProjectRepository
{
    Task<IReadOnlyCollection<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Project>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
