using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.DTOs.Tasks;

public sealed class TaskDto
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? SprintId { get; init; }
    public TaskType Type { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string? AssignedToUserId { get; init; }
    public string AssignedUserName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int StoryPoints { get; init; }
    public int? SprintOrder { get; init; }
    public DateTime? Deadline { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
}