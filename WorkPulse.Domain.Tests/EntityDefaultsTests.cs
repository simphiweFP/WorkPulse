using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Tests;

public class EntityDefaultsTests
{
    [Fact]
    public void TaskItem_ShouldDefaultToTodoStatus()
    {
        var task = new TaskItem();

        Assert.Equal(TaskStatus.Todo, task.Status);
    }

    [Fact]
    public void Project_ShouldDefaultToActiveStatus()
    {
        var project = new Project();

        Assert.Equal(ProjectStatus.Active, project.Status);
    }
}
