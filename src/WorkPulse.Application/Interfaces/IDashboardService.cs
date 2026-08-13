using WorkPulse.Application.DTOs.Dashboard;

namespace WorkPulse.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
