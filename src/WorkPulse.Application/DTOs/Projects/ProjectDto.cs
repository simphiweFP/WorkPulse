using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Projects;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public ProjectStatus Status { get; init; }
}
