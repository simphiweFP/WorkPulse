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
        Assert.Equal("Critical priority with deadline approaching", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldRankCriticalTaskCorrectly()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("low-future", 5, TaskPriority.Low),
            Task("critical-today", 0, TaskPriority.Critical),
            Task("high-tomorrow", 1, TaskPriority.High)
        ], Today);

        Assert.Equal("critical-today", ranked.First().Title);
        Assert.Equal("Critical priority and due today", ranked.First().RecommendationReason);
    }

    [Fact]
    public void ShouldExcludeCompletedTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("done", 0, TaskPriority.Critical, TaskStatus.Completed)], Today);

        Assert.Empty(ranked);
    }

    [Fact]
    public void ShouldExcludeLowPriorityFutureTask()
    {
        var ranked = _service.RankDeveloperTasks([Task("future", 10, TaskPriority.Low)], Today);

        Assert.Empty(ranked);
    }

    [Fact]
    public void ShouldRankOverdueTaskFirst()
    {
        var ranked = _service.RankDeveloperTasks([
            Task("critical-future", 7, TaskPriority.Critical),
            Task("overdue", -1, TaskPriority.Low),
            Task("today", 0, TaskPriority.Medium)
        ], Today);

        Assert.Equal("overdue", ranked.First().Title);
    }

    [Fact]
    public void ShouldGenerateRecommendationReason()
    {
        var ranked = _service.RankDeveloperTasks([Task("reason", 1, TaskPriority.High)], Today);

        Assert.Single(ranked);
        Assert.Equal("High priority and due tomorrow", ranked.First().RecommendationReason);
    }

    private static TodayTaskCandidate Task(string title, int dueOffsetDays, TaskPriority priority, TaskStatus status = TaskStatus.Todo) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = Guid.NewGuid(),
        ClientId = Guid.NewGuid(),
        Title = title,
        Description = title,
        ClientName = "Client",
        ProjectName = "Project",
        Priority = priority,
        DueDate = Today.AddDays(dueOffsetDays),
        Status = status
    };
}