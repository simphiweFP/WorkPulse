using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Tests;

public class TaskItemInvariantsTests
{
    [Fact]
    public void AssignTo_ShouldRejectCompletedTask()
    {
        var task = new TaskItem { Status = TaskStatus.Completed };

        var exception = Assert.Throws<InvalidOperationException>(() => task.AssignTo("dev-1", DateTime.UtcNow));

        Assert.Equal("Completed tasks cannot be reassigned.", exception.Message);
    }

    [Fact]
    public void ChangeStatus_ShouldRejectReopeningCompletedTask()
    {
        var task = new TaskItem { Status = TaskStatus.Completed, CompletedAt = DateTime.UtcNow };

        var exception = Assert.Throws<InvalidOperationException>(() => task.ChangeStatus(TaskStatus.Todo, DateTime.UtcNow));

        Assert.Equal("Completed tasks cannot be reopened.", exception.Message);
    }

    [Fact]
    public void Project_ShouldOnlyAcceptNewTasks_WhenActive()
    {
        var activeProject = new Project { Status = ProjectStatus.Active };
        var closedProject = new Project { Status = ProjectStatus.Completed };

        Assert.True(activeProject.CanAcceptNewTasks());
        Assert.False(closedProject.CanAcceptNewTasks());
    }
}