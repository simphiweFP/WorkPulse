using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Services;

public sealed class TodayTaskService : ITodayTaskService
{
    private const int MaxUpcomingDaysToInclude = 3;
    private const int OverdueScore = 100;
    private const int DueTodayScore = 80;
    private const int DueTomorrowScore = 50;
    private const int DueInTwoDaysScore = 40;
    private const int DueInThreeDaysScore = 30;

    private const int CriticalScore = 40;
    private const int HighScore = 30;
    private const int MediumScore = 20;
    private const int LowScore = 10;

    public IReadOnlyCollection<TodayTaskCandidate> RankDeveloperTasks(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
    {
        var today = utcToday.Date;
        return tasks
            .Where(task => task.Status != TaskStatus.Completed)
            .Where(task => ShouldInclude(task, today))
            .Select(task => ApplyScoring(task, today))
            .Where(task => task.Score > 0)
            .OrderByDescending(task => task.Score)
            .ThenBy(task => task.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.Title)
            .ThenBy(task => task.Id)
            .ToArray();
    }

    public TodaySummary BuildSummary(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
    {
        var today = utcToday.Date;
        var list = tasks.Where(task => task.Status != TaskStatus.Completed).ToArray();
        return new TodaySummary
        {
            Total = list.Length,
            Overdue = list.Count(task => task.DueDate.HasValue && task.DueDate.Value.Date < today),
            DueToday = list.Count(task => task.DueDate.HasValue && task.DueDate.Value.Date == today),
            HighPriority = list.Count(task => task.Priority is TaskPriority.High or TaskPriority.Critical)
        };
    }

    public IReadOnlyCollection<TodayTaskCandidate> GetAdminSnapshot(IEnumerable<TodayTaskCandidate> tasks, DateTime utcToday)
        => RankDeveloperTasks(tasks, utcToday);

    private static bool ShouldInclude(TodayTaskCandidate task, DateTime today)
    {
        if (!task.DueDate.HasValue)
        {
            return task.Priority is TaskPriority.High or TaskPriority.Critical;
        }

        var due = task.DueDate.Value.Date;
        var days = (due - today).Days;

        if (days <= MaxUpcomingDaysToInclude)
        {
            return true;
        }

        return task.Priority is TaskPriority.High or TaskPriority.Critical;
    }

    private static TodayTaskCandidate ApplyScoring(TodayTaskCandidate task, DateTime today)
    {
        var score = 0;
        var reason = string.Empty;

        if (!task.DueDate.HasValue)
        {
            score += PriorityScore(task.Priority);
            reason = task.Priority switch
            {
                TaskPriority.Critical => "Critical priority with no deadline",
                TaskPriority.High => "High priority with no deadline",
                _ => PriorityReason(task.Priority)
            };
            return task with { Score = score, RecommendationReason = reason };
        }

        var due = task.DueDate.Value.Date;
        var days = (due - today).Days;
        if (days < 0)
        {
            score += OverdueScore;
            reason = $"Overdue by {Math.Abs(days)} day{(Math.Abs(days) == 1 ? string.Empty : "s")}";
        }
        else if (days == 0)
        {
            score += DueTodayScore;
            reason = "Due today";
        }
        else if (days == 1)
        {
            score += DueTomorrowScore;
            reason = "Due tomorrow";
        }
        else if (days == 2)
        {
            score += DueInTwoDaysScore;
            reason = "Due in 2 days";
        }
        else if (days == 3)
        {
            score += DueInThreeDaysScore;
            reason = "Due in 3 days";
        }
        else if (task.Priority is TaskPriority.High or TaskPriority.Critical)
        {
            reason = task.Priority switch
            {
                TaskPriority.Critical => "Critical priority upcoming",
                TaskPriority.High => "High priority upcoming",
                _ => string.Empty
            };
        }

        score += PriorityScore(task.Priority);
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = PriorityReason(task.Priority);
        }
        else
        {
            reason = task.Priority switch
            {
                TaskPriority.Critical when days == 0 => "Critical priority and due today",
                TaskPriority.High when days == 1 => "High priority and due tomorrow",
                TaskPriority.Critical => "Critical priority with deadline approaching",
                TaskPriority.High => "High priority with deadline approaching",
                _ => reason
            };
        }

        return task with { Score = score, RecommendationReason = reason };
    }

    private static int PriorityScore(TaskPriority priority)
        => priority switch
        {
            TaskPriority.Critical => CriticalScore,
            TaskPriority.High => HighScore,
            TaskPriority.Medium => MediumScore,
            TaskPriority.Low => LowScore,
            _ => LowScore
        };

    private static string PriorityReason(TaskPriority priority)
        => priority switch
        {
            TaskPriority.Critical => "Critical priority",
            TaskPriority.High => "High priority",
            TaskPriority.Medium => "Medium priority",
            _ => "Low priority"
        };
}