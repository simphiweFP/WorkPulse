using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Services;

public sealed record TodayTaskCandidate
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateTime? DueDate { get; init; }
    public TaskStatus Status { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
    public int Score { get; init; }
}