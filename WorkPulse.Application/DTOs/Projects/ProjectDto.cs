using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Projects;

public sealed class ProjectDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public int OpenTaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
}