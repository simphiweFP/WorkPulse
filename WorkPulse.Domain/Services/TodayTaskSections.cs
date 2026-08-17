namespace WorkPulse.Domain.Services;

public sealed record TodayTaskSections
{
    public TodayTaskCandidate? TopPriority { get; init; }
    public IReadOnlyCollection<TodayTaskCandidate> Overdue { get; init; } = Array.Empty<TodayTaskCandidate>();
    public IReadOnlyCollection<TodayTaskCandidate> DueToday { get; init; } = Array.Empty<TodayTaskCandidate>();
    public IReadOnlyCollection<TodayTaskCandidate> RecommendedNext { get; init; } = Array.Empty<TodayTaskCandidate>();
    public IReadOnlyCollection<TodayTaskCandidate> CompletedToday { get; init; } = Array.Empty<TodayTaskCandidate>();
    public IReadOnlyCollection<TodayTaskCandidate> AllActiveOrdered { get; init; } = Array.Empty<TodayTaskCandidate>();
}
