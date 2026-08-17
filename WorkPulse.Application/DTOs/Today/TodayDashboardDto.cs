namespace WorkPulse.Application.DTOs.Today;

public sealed class TodayDashboardDto
{
    public TodaySummaryDto Summary { get; init; } = new();
    public TodayTaskDto TopPriority { get; init; } = new();
    public IReadOnlyCollection<TodayTaskDto> Overdue { get; init; } = Array.Empty<TodayTaskDto>();
    public IReadOnlyCollection<TodayTaskDto> DueToday { get; init; } = Array.Empty<TodayTaskDto>();
    public IReadOnlyCollection<TodayTaskDto> RecommendedNext { get; init; } = Array.Empty<TodayTaskDto>();
    public IReadOnlyCollection<TodayTaskDto> CompletedToday { get; init; } = Array.Empty<TodayTaskDto>();
    public bool SprintWorkComplete { get; init; }
    public string SprintName { get; init; } = string.Empty;
    public int SprintCompletedTasks { get; init; }
    public int SprintTotalTasks { get; init; }
    public int SprintCompletedPoints { get; init; }
    public int SprintTotalPoints { get; init; }
}
