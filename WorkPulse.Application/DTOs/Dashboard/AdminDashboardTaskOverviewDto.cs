namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class AdminDashboardTaskOverviewDto
{
    public int Overdue { get; init; }
    public int DueToday { get; init; }
    public int InProgress { get; init; }
    public int Completed { get; init; }
}
