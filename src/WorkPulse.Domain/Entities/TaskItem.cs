using WorkPulse.Domain.Enums;

namespace WorkPulse.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? Deadline { get; set; }
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Todo;
    public string? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ApplicationUser? AssignedToUser { get; set; }
}
