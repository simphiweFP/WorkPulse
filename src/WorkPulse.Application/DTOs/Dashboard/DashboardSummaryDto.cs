namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class DashboardSummaryDto
{
    public int TasksDueToday { get; init; }
    public int OverdueTasks { get; init; }
    public int TasksCompleted { get; init; }
    public int OpenTasks { get; init; }
    public int HighPriorityTasks { get; init; }
    public int ActiveProjects { get; init; }
    public int MyAssignedTasks { get; init; }
}
