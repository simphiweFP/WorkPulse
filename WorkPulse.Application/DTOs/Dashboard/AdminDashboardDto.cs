namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class AdminDashboardDto
{
    public AdminDashboardSummaryDto Summary { get; init; } = new();
    public AdminDashboardTaskOverviewDto TaskOverview { get; init; } = new();
    public AdminDashboardPriorityBreakdownDto PriorityBreakdown { get; init; } = new();
    public IReadOnlyCollection<AdminDashboardRecentTaskDto> RecentTasks { get; init; } = Array.Empty<AdminDashboardRecentTaskDto>();
}
