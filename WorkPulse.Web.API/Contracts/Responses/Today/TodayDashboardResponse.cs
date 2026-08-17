namespace WorkPulse.Web.API.Contracts.Responses.Today;

public sealed class TodayDashboardResponse
{
    public TodaySummaryResponse Summary { get; init; } = new();
    public TodayTaskResponse TopPriority { get; init; } = new();
    public IReadOnlyCollection<TodayTaskResponse> Overdue { get; init; } = Array.Empty<TodayTaskResponse>();
    public IReadOnlyCollection<TodayTaskResponse> DueToday { get; init; } = Array.Empty<TodayTaskResponse>();
    public IReadOnlyCollection<TodayTaskResponse> RecommendedNext { get; init; } = Array.Empty<TodayTaskResponse>();
    public IReadOnlyCollection<TodayTaskResponse> CompletedToday { get; init; } = Array.Empty<TodayTaskResponse>();
    public bool SprintWorkComplete { get; init; }
    public string? SprintName { get; init; }
    public int SprintCompletedTasks { get; init; }
    public int SprintTotalTasks { get; init; }
    public int SprintCompletedPoints { get; init; }
    public int SprintTotalPoints { get; init; }
}
