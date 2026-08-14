using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Web.API.Contracts.Responses.Today;

public sealed class TodayTaskResponse
{
    public Guid Id { get; init; }
    public Guid ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
    public TaskStatus Status { get; init; }
    public TaskPriority Priority { get; init; }
    public string RecommendationReason { get; init; } = string.Empty;
    public int Score { get; init; }
}
