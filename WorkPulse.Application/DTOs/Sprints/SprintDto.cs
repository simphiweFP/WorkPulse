using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Sprints;

public sealed class SprintDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public SprintStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int TaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
}
