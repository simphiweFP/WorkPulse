using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyCollection<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TaskDto>> GetMyTasksAsync(string userId, GetMyTasksFilterDto filter, CancellationToken cancellationToken = default);
    Task<TaskDto> CreateAsync(CreateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateTaskRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid id, AssignTaskRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, string currentUserId, bool isAdmin, TaskStatus status, CancellationToken cancellationToken = default);
    Task CompleteAsync(Guid id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}