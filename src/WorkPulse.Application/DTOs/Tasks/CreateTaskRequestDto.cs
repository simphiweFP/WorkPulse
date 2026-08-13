using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.DTOs.Tasks;

public sealed class CreateTaskRequestDto
{
    [Required]
    public Guid ProjectId { get; init; }
    public string? AssignedUserId { get; init; }

    [Required]
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public TaskStatus Status { get; init; } = TaskStatus.Todo;
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;
}
