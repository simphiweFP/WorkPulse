using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.DTOs.Tasks;

public sealed class TaskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string? AssignedUserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
}
