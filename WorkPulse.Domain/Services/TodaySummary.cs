namespace WorkPulse.Domain.Services;

public sealed class TodaySummary
{
    public int Total { get; init; }
    public int Overdue { get; init; }
    public int DueToday { get; init; }
    public int HighPriority { get; init; }
}