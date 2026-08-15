namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class AdminDashboardPriorityBreakdownDto
{
    public int Critical { get; init; }
    public int High { get; init; }
    public int Medium { get; init; }
    public int Low { get; init; }
}
