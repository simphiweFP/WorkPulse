using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Services;

public sealed class TodayTaskService : ITodayTaskService
{
    private const int CriticalScore = 40;
    private const int HighScore = 30;
    private const int MediumScore = 20;
    private const int LowScore = 10;

    public IReadOnlyCollection<TodayTaskCandidate> GetEligibleTasks(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
        => GetActiveTasks(tasks).ToArray();

    public IReadOnlyCollection<TodayTaskCandidate> RankDeveloperTasks(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
        => BuildSections(tasks, utcToday).AllActiveOrdered;

    public TodaySummary BuildSummary(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
    {
        var today = utcToday.Date;
        var activeTasks = GetActiveTasks(tasks).ToArray();
        return new TodaySummary
        {
            Total = activeTasks.Length,
            Overdue = activeTasks.Count(task => IsOverdue(task, today)),
            DueToday = activeTasks.Count(task => IsDueToday(task, today)),
            HighPriority = activeTasks.Count(task => task.Priority is TaskPriority.High or TaskPriority.Critical)
        };
    }

    public TodayTaskSections BuildSections(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
    {
        var today = utcToday.Date;
        var activeTasks = GetActiveTasks(tasks).ToArray();
        var completedTasks = tasks
            .Where(task => task.Status == TaskStatus.Completed)
            .Select(task => task with { RecommendationReason = "Recently completed", Score = 0 })
            .OrderByDescending(task => task.SprintOrder ?? int.MinValue)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .ToArray();

        var topPriority = SelectTopPriority(activeTasks, today);
        var remainingActive = topPriority is null
            ? activeTasks
            : activeTasks.Where(task => task.Id != topPriority.Id).ToArray();

        var overdue = remainingActive
            .Where(task => IsOverdue(task, today))
            .Select(task => ApplyReason(task, today))
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate ?? DateTime.MinValue)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .ToArray();

        var dueToday = remainingActive
            .Where(task => IsDueToday(task, today))
            .Select(task => ApplyReason(task, today))
            .OrderByDescending(task => task.Status == TaskStatus.InProgress)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .ToArray();

        var hasAnyDatedActiveTask = activeTasks.Any(task => task.DueDate.HasValue);
        var recommendedNextSource = hasAnyDatedActiveTask
            ? remainingActive.Where(task => IsUpcoming(task, today))
            : remainingActive.Where(task => !task.DueDate.HasValue);

        var recommendedNext = recommendedNextSource
            .Select(task => ApplyReason(task, today))
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Status == TaskStatus.InProgress)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .ToArray();

        var topPriorityWithReason = topPriority is null ? null : ApplyReason(topPriority, today);

        var allActiveOrdered = topPriorityWithReason is null
            ? overdue.Concat(dueToday).Concat(recommendedNext).ToArray()
            : new[] { topPriorityWithReason }.Concat(overdue).Concat(dueToday).Concat(recommendedNext).ToArray();

        return new TodayTaskSections
        {
            TopPriority = topPriorityWithReason,
            Overdue = overdue,
            DueToday = dueToday,
            RecommendedNext = recommendedNext,
            CompletedToday = completedTasks,
            AllActiveOrdered = allActiveOrdered
        };
    }

    public IReadOnlyCollection<TodayTaskCandidate> GetAdminSnapshot(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
        => RankDeveloperTasks(tasks, utcToday);

    private static IEnumerable<TodayTaskCandidate> GetActiveTasks(IEnumerable<TodayTaskCandidate> tasks)
        => tasks.Where(IsActive);

    private static bool IsActive(TodayTaskCandidate task)
        => task.Status is TaskStatus.Todo or TaskStatus.InProgress;

    private static bool IsOverdue(TodayTaskCandidate task, DateTime today)
        => task.DueDate.HasValue && task.DueDate.Value.Date < today;

    private static bool IsDueToday(TodayTaskCandidate task, DateTime today)
        => task.DueDate.HasValue && task.DueDate.Value.Date == today;

    private static bool IsUpcoming(TodayTaskCandidate task, DateTime today)
        => task.DueDate.HasValue && task.DueDate.Value.Date > today;

    private static TodayTaskCandidate? SelectTopPriority(IEnumerable<TodayTaskCandidate> tasks, DateTime today)
    {
        var inProgressOverdue = tasks.Where(task => task.Status == TaskStatus.InProgress && IsOverdue(task, today))
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate ?? DateTime.MinValue)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (inProgressOverdue is not null)
        {
            return inProgressOverdue;
        }

        var overdue = tasks.Where(task => IsOverdue(task, today))
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate ?? DateTime.MinValue)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (overdue is not null)
        {
            return overdue;
        }

        var inProgressDueToday = tasks.Where(task => task.Status == TaskStatus.InProgress && IsDueToday(task, today))
            .OrderBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (inProgressDueToday is not null)
        {
            return inProgressDueToday;
        }

        var dueToday = tasks.Where(task => IsDueToday(task, today))
            .OrderByDescending(task => task.Status == TaskStatus.InProgress)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (dueToday is not null)
        {
            return dueToday;
        }

        var inProgressUpcoming = tasks.Where(task => task.Status == TaskStatus.InProgress && IsUpcoming(task, today))
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (inProgressUpcoming is not null)
        {
            return inProgressUpcoming;
        }

        var upcoming = tasks.Where(task => IsUpcoming(task, today))
            .OrderBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
        if (upcoming is not null)
        {
            return upcoming;
        }

        return tasks.Where(task => !task.DueDate.HasValue)
            .OrderByDescending(task => task.Status == TaskStatus.InProgress)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .FirstOrDefault();
    }

    private static TodayTaskCandidate ApplyReason(TodayTaskCandidate task, DateTime today)
    {
        var reason = BuildReason(task, today);
        var score = Score(task);
        return task with { RecommendationReason = reason, Score = score };
    }

    private static string BuildReason(TodayTaskCandidate task, DateTime today)
    {
        if (!task.DueDate.HasValue)
        {
            return task.Status == TaskStatus.InProgress ? "In progress · no deadline set" : "No deadline set";
        }

        var due = task.DueDate.Value.Date;
        var days = (due - today).Days;
        var baseReason = days switch
        {
            < 0 => $"Overdue by {Math.Abs(days)} day{(Math.Abs(days) == 1 ? string.Empty : "s")}",
            0 => "Due today",
            1 => "Due tomorrow",
            _ => $"Due in {days} days"
        };

        return task.Status == TaskStatus.InProgress ? $"In progress · {baseReason.ToLowerInvariant()}" : baseReason;
    }

    private static int Score(TodayTaskCandidate task)
        => task.Priority switch
        {
            TaskPriority.Critical => CriticalScore,
            TaskPriority.High => HighScore,
            TaskPriority.Medium => MediumScore,
            _ => LowScore
        };
}
