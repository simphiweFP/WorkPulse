using WorkPulse.Application.DTOs.Today;

namespace WorkPulse.Application.Interfaces;

public interface ITodayService
{
    Task<TodayDashboardDto> GetMyTodayAsync(string userId, CancellationToken cancellationToken = default);
    Task<TodayDashboardDto> GetAdminTodayAsync(string userId, CancellationToken cancellationToken = default);
}
