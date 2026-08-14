namespace WorkPulse.Application.DTOs.Today;

public sealed class TodayDashboardDto
{
    public TodaySummaryDto Summary { get; init; } = new();
    public IReadOnlyCollection<TodayTaskDto> Tasks { get; init; } = Array.Empty<TodayTaskDto>();
}
