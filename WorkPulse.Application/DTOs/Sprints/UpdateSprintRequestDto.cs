using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Sprints;

public sealed class UpdateSprintRequestDto
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public SprintStatus Status { get; init; }
    public int TotalTasks { get; init; }
}
