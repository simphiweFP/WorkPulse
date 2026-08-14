using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.Interfaces;

public interface ITaskRepository
{
    Task<IReadOnlyCollection<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TaskItem>> GetMyTasksAsync(string userId, TaskStatus? status, TaskPriority? priority, Guid? projectId, DateTime? dueDate, CancellationToken cancellationToken = default);
    Task CreateAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid taskId, string? userId, CancellationToken cancellationToken = default);
}