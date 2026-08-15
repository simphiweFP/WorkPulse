using WorkPulse.Application.DTOs.Sprints;

namespace WorkPulse.Application.Interfaces;

public interface ISprintService
{
    Task<IReadOnlyCollection<SprintDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SprintDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SprintDto> CreateAsync(CreateSprintRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateSprintRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
