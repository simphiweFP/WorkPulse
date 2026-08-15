using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Web.API.Contracts.Requests.Tasks;

public sealed class CreateTaskRequest
{
    [Required]
    public Guid ProjectId { get; init; }

    public Guid? SprintId { get; init; }

    public string? AssignedToUserId { get; init; }

    [Required]
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
    public DateTime? Deadline { get; init; }
    public TaskStatus Status { get; init; } = TaskStatus.Todo;
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
}