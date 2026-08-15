namespace WorkPulse.Application.DTOs.Dashboard;

public sealed class AdminDashboardSummaryDto
{
    public int Clients { get; init; }
    public int Projects { get; init; }
    public int Tasks { get; init; }
    public int TeamMembers { get; init; }
}
