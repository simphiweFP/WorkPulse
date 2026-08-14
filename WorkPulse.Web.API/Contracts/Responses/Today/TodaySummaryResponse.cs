namespace WorkPulse.Web.API.Contracts.Responses.Today;

public sealed class TodaySummaryResponse
{
    public int Total { get; init; }
    public int Overdue { get; init; }
    public int DueToday { get; init; }
    public int HighPriority { get; init; }
}
