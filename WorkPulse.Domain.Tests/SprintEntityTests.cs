using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Domain.Tests;

public class SprintEntityTests
{
    [Fact]
    public void Sprint_DefaultStatus_ShouldBePlanned()
    {
        var sprint = new Sprint();

        Assert.Equal(SprintStatus.Planned, sprint.Status);
    }
}
