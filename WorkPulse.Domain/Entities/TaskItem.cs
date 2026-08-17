using WorkPulse.Domain.Enums;

namespace WorkPulse.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? SprintId { get; set; }
    public TaskType Type { get; set; } = TaskType.Story;
    public string SprintName { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int StoryPoints { get; set; }
    public int? SprintOrder { get; set; }
    public DateTime? Deadline { get; set; }
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Todo;
    public string? AssignedToUserId { get; set; }
    public string AssignedUserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted => Status == Enums.TaskStatus.Completed;

    public bool IsScheduledForToday(DateTime utcNow)
    {
        var today = utcNow.Date;
        return Deadline.HasValue
            && Deadline.Value.Date == today
            && !IsCompleted;
    }

    public void AssignTo(string? userId, DateTime utcNow)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Completed tasks cannot be reassigned.");
        }

        AssignedToUserId = userId;
        UpdatedAt = utcNow;
    }

    public void ChangeStatus(Enums.TaskStatus newStatus, DateTime utcNow)
    {
        if (Status == Enums.TaskStatus.Completed && newStatus != Enums.TaskStatus.Completed)
        {
            throw new InvalidOperationException("Completed tasks cannot be reopened.");
        }

        if (Status == newStatus)
        {
            return;
        }

        if (Status == Enums.TaskStatus.Todo && newStatus == Enums.TaskStatus.InProgress)
        {
            Status = newStatus;
            UpdatedAt = utcNow;
            return;
        }

        if (Status is Enums.TaskStatus.Todo or Enums.TaskStatus.InProgress && newStatus == Enums.TaskStatus.Completed)
        {
            Status = newStatus;
            CompletedAt ??= utcNow;
            UpdatedAt = utcNow;
            return;
        }

        if (Status == Enums.TaskStatus.InProgress && newStatus == Enums.TaskStatus.Todo)
        {
            Status = newStatus;
            UpdatedAt = utcNow;
            return;
        }

        throw new InvalidOperationException("Invalid task status transition.");
    }

    public Project Project { get; set; } = null!;
}
