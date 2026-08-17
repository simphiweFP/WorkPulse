using WorkPulse.Domain.Entities;

namespace WorkPulse.Domain.Services;

public interface ITodayTaskService
{
    IReadOnlyCollection<TodayTaskCandidate> GetEligibleTasks(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday);
    IReadOnlyCollection<TodayTaskCandidate> RankDeveloperTasks(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday);
    TodaySummary BuildSummary(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday);
    TodayTaskSections BuildSections(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday);
    IReadOnlyCollection<TodayTaskCandidate> GetAdminSnapshot(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday);
}