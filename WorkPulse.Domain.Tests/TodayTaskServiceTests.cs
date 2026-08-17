using WorkPulse.Domain.Enums;
using WorkPulse.Domain.Services;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Tests;

public class TodayTaskServiceTests
{
    private readonly TodayTaskService _service = new();
    private static readonly DateTime Today = new(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ShouldIncludeOverdueTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("overdue", -1, TaskPriority.Low)], Today);

        Assert.Single(ranked);
        Assert.Equal("Overdue by 1 day", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldKeepOverdueReasonForOverdueCriticalTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("overdue-critical", -2, TaskPriority.Critical)], Today);

        Assert.Single(ranked);
        Assert.StartsWith("Overdue by 2 days", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldIncludeTaskDueToday()
    {
        var ranked = _service.RankDeveloperTasks([Task("today", 0, TaskPriority.Medium)], Today);

        Assert.Single(ranked);
        Assert.Equal("Due today", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldIncludeCriticalUpcomingTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("critical", 10, TaskPriority.Critical)], Today);

        Assert.Single(ranked);
        Assert.Equal("Due in 10 days", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldRankCriticalTaskCorrectly()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("low-future", 5, TaskPriority.Low, sprintOrder: 3),
            Task("critical-today", 0, TaskPriority.Critical, sprintOrder: 1),
            Task("high-tomorrow", 1, TaskPriority.High, sprintOrder: 2)
        ], Today);

        Assert.Equal("critical-today", ranked.First().Title);
        Assert.Equal("Due today", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldExcludeCompletedTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("done", 0, TaskPriority.Critical, TaskStatus.Completed)], Today);

        Assert.Empty(ranked);
    }

    [Fact]
    public void ShouldSelectInProgressOverdueTaskAsTopPriority()
    {
        var sections = _service.BuildSections([
            Task("todo-overdue", -2, TaskPriority.High, TaskStatus.Todo),
            Task("in-progress-overdue", -1, TaskPriority.Medium, TaskStatus.InProgress),
            Task("due-today", 0, TaskPriority.Critical, TaskStatus.InProgress)
        ], Today);

        Assert.Equal("in-progress-overdue", sections.TopPriority?.Title);
        Assert.DoesNotContain(sections.Overdue, task => task.Title == "in-progress-overdue");
        Assert.DoesNotContain(sections.DueToday, task => task.Title == "in-progress-overdue");
    }

    [Fact]
    public void ShouldUseNoDeadlineTaskOnlyWhenNoDatedActiveTaskExists()
    {
        var sections = _service.BuildSections([
            Task("upcoming", 3, TaskPriority.Low, TaskStatus.Todo)
        ], Today);

        Assert.Equal("upcoming", sections.TopPriority?.Title);
    }

    [Fact]
    public void ShouldUseNoDeadlineTaskWhenNoDatedActiveTaskExists()
    {
        var sections = _service.BuildSections([
            Task("no-deadline", 0, TaskPriority.High, TaskStatus.Todo, noDueDate: true)
        ], Today);

        Assert.Equal("no-deadline", sections.TopPriority?.Title);
        Assert.Empty(sections.RecommendedNext);
    }

    [Fact]
    public void ShouldShowClearReasonForInProgressUpcomingTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("in-progress-upcoming", 1, TaskPriority.High, TaskStatus.InProgress)], Today);

        Assert.Single(ranked);
        Assert.Equal("In progress · due tomorrow", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldShowNoDeadlineReasonForActiveTaskWithoutDeadline()
    {
        var ranked = _service.RankDeveloperTasks([Task("no-deadline", 0, TaskPriority.Medium, TaskStatus.Todo, noDueDate: true)], Today);

        Assert.Single(ranked);
        Assert.Equal("No deadline set", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldExcludeLowPriorityFutureTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("future", 10, TaskPriority.Low)], Today);

        Assert.Single(ranked);
        Assert.Equal("Due in 10 days", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldRankOverdueTaskFirst()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("critical-future", 7, TaskPriority.Critical, sprintOrder: 2),
            Task("overdue", -1, TaskPriority.Low, sprintOrder: 3),
            Task("today", 0, TaskPriority.Medium, sprintOrder: 1)
        ], Today);

        Assert.Equal("overdue", ranked.First().Title);
    }

    [Fact]
    public void ShouldGenerateRecommendationReason()
    {
        var ranked = _service.RankDeveloperTasks([Task("reason", 1, TaskPriority.High, sprintOrder: 1)], Today);

        Assert.Single(ranked);
        Assert.Equal("Due tomorrow", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldPreferInProgressOverTodoWhenOtherwiseSimilar()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("todo", 0, TaskPriority.High, TaskStatus.Todo, sprintOrder: 2),
            Task("in-progress", 0, TaskPriority.High, TaskStatus.InProgress, sprintOrder: 1)
        ], Today);

        Assert.Equal("in-progress", ranked.First().Title);
    }

    [Fact]
    public void ShouldUseSprintOrderAsTieBreaker()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("later", 2, TaskPriority.High, sprintOrder: 2),
            Task("earlier", 2, TaskPriority.High, sprintOrder: 1)
        ], Today);

        Assert.Equal("earlier", ranked.First().Title);
    }

    [Fact]
    public void ShouldExcludeCompletedActiveSprintTasksFromToday()
    {
        var tasks = new[]
        {
            Task("done-1", 1, TaskPriority.High, TaskStatus.Completed, sprintId: Guid.NewGuid(), sprintStartOffsetDays: -1, sprintEndOffsetDays: 7),
            Task("done-2", 2, TaskPriority.High, TaskStatus.Completed, sprintId: Guid.NewGuid(), sprintStartOffsetDays: -1, sprintEndOffsetDays: 7)
        };

        var ranked = _service.RankDeveloperTasks(tasks, Today);
        var summary = _service.BuildSummary(tasks, Today);

        Assert.Empty(ranked);
        Assert.Equal(0, summary.Total);
    }

    [Fact]
    public void ShouldExcludeFutureSprintTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("future-sprint", 2, TaskPriority.High, sprintStartOffsetDays: 1, sprintEndOffsetDays: 7)], Today);

        Assert.Single(ranked);
        Assert.Equal("Due in 2 days", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldIncludeSprintStartingToday()
    {
        var ranked = _service.RankDeveloperTasks([Task("starts-today", 0, TaskPriority.High, sprintStartOffsetDays: 0, sprintEndOffsetDays: 7)], Today);

        Assert.Single(ranked);
    }

    [Fact]
    public void ShouldIncludeActiveSprintTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("active", 1, TaskPriority.High, sprintStartOffsetDays: -3, sprintEndOffsetDays: 2)], Today);

        Assert.Single(ranked);
    }

    [Fact]
    public void ShouldExcludeEndedSprintNormalTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("ended", 4, TaskPriority.High, sprintStartOffsetDays: -10, sprintEndOffsetDays: -1)], Today);

        Assert.Single(ranked);
    }

    [Fact]
    public void ShouldIncludeCarryOverOverdueTaskFromEndedSprint()
    {
        var ranked = _service.RankDeveloperTasks([Task("carry-over", -2, TaskPriority.High, sprintStartOffsetDays: -10, sprintEndOffsetDays: -1)], Today);

        Assert.Single(ranked);
        Assert.Equal("Overdue by 2 days", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldExcludeCompletedTaskFromActiveToday()
    {
        var ranked = _service.RankDeveloperTasks([Task("completed", 0, TaskPriority.High, TaskStatus.Completed, sprintStartOffsetDays: -1, sprintEndOffsetDays: 1)], Today);

        Assert.Empty(ranked);
    }

    [Fact]
    public void SummaryShouldMatchEligibleDataset()
    {
        var tasks = new[]
        {
            Task("overdue", -1, TaskPriority.High, sprintStartOffsetDays: -2, sprintEndOffsetDays: 2),
            Task("today", 0, TaskPriority.High, sprintStartOffsetDays: -2, sprintEndOffsetDays: 2),
            Task("future", 5, TaskPriority.Low, sprintStartOffsetDays: 1, sprintEndOffsetDays: 7)
        };

        var summary = _service.BuildSummary(tasks, Today);

        Assert.Equal(3, summary.Total);
        Assert.Equal(1, summary.Overdue);
        Assert.Equal(1, summary.DueToday);
        Assert.Equal(2, summary.HighPriority);
    }

    private static TodayTaskCandidate Task(string title, int dueOffsetDays, TaskPriority priority, TaskStatus status = TaskStatus.Todo, int? sprintOrder = null, int? sprintStartOffsetDays = null, int? sprintEndOffsetDays = null, Guid? sprintId = null, bool noDueDate = false) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        ClientId = Guid.NewGuid(),
        Title = title,
        Description = title,
        ClientName = "Client",
        ProjectName = "Project",
        Priority = priority,
        DueDate = noDueDate ? null : Today.AddDays(dueOffsetDays),
        Status = status,
        SprintOrder = sprintOrder,
        SprintId = sprintId ?? (sprintStartOffsetDays.HasValue || sprintEndOffsetDays.HasValue ? Guid.NewGuid() : null),
        SprintStartDate = sprintStartOffsetDays.HasValue ? Today.AddDays(sprintStartOffsetDays.Value) : null,
        SprintEndDate = sprintEndOffsetDays.HasValue ? Today.AddDays(sprintEndOffsetDays.Value) : null,
        SprintName = "Sprint"
    };
}