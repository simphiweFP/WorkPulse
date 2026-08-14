namespace WorkPulse.Application.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetDueTodayCountAsync(DateTime utcTodayStart, DateTime utcTomorrowStart, CancellationToken cancellationToken = default);
    Task<int> GetOverdueCountAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetHighPriorityCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetActiveProjectsCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetMyAssignedTasksCountAsync(string userId, CancellationToken cancellationToken = default);
}