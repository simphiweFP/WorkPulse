using WorkPulse.Application.DTOs.Dashboard;

namespace WorkPulse.Application.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}
