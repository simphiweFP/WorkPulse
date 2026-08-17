using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Services;

public sealed record TodayTaskCandidate
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ClientId { get; init; }
    public Guid? SprintId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public TaskType Type { get; init; }
    public TaskPriority Priority { get; init; }
    public int? SprintOrder { get; init; }
    public DateTime? SprintStartDate { get; init; }
    public DateTime? SprintEndDate { get; init; }
    public SprintStatus? SprintStatus { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public int StoryPoints { get; init; }
    public TaskStatus Status { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
    public int Score { get; init; }
}