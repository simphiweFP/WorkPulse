namespace WorkPulse.Web.API.Contracts.Responses.Today;

public sealed class TodayDashboardResponse
{
    public TodaySummaryResponse Summary { get; init; } = new();
    public IReadOnlyCollection<TodayTaskResponse> Tasks { get; init; } = Array.Empty<TodayTaskResponse>();
}
