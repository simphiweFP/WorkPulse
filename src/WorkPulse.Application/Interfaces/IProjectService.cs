using WorkPulse.Application.DTOs.Projects;

namespace WorkPulse.Application.Interfaces;

public interface IProjectService
{
    Task<IReadOnlyCollection<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProjectDto>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateProjectRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
