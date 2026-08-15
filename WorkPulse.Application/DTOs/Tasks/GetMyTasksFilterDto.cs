using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.DTOs.Tasks;

public sealed class GetMyTasksFilterDto
{
    public TaskStatus? Status { get; init; }
    public TaskPriority? Priority { get; init; }
    public Guid? ProjectId { get; init; }
    public DateTime? Deadline { get; init; }
}