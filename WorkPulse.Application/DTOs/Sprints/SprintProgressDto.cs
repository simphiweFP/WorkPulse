namespace WorkPulse.Application.DTOs.Sprints;

public sealed class SprintProgressDto
{
    public int TaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
    public int TotalPoints { get; init; }
    public int CompletedPoints { get; init; }

    public bool HasTasks => TaskCount > 0;
}