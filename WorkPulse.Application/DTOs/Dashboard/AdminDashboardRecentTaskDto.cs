using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;
using TaskPriority = WorkPulse.Domain.Enums.TaskPriority;

namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class AdminDashboardRecentTaskDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string AssigneeName { get; init; } = string.Empty;
    public TaskPriority Priority { get; init; }
    public DateTime? Deadline { get; init; }
    public TaskStatus Status { get; init; }
}
