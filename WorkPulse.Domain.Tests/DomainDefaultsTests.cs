using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Domain.Tests;

public class DomainDefaultsTests
{
    [Fact]
    public void Project_DefaultStatus_ShouldBeActive()
    {
        var project = new Project();

        Assert.Equal(ProjectStatus.Active, project.Status);
    }

    [Fact]
    public void TaskItem_DefaultStatus_ShouldBeTodo()
    {
        var task = new TaskItem();

        Assert.Equal(TaskStatus.Todo, task.Status);
    }
}
