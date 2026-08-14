namespace WorkPulse.Application.DTOs.Today;

public sealed class TodaySummaryDto
{
    public int Total { get; init; }
    public int Overdue { get; init; }
    public int DueToday { get; init; }
    public int HighPriority { get; init; }
}
