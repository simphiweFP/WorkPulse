using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Projects;

public sealed class CreateProjectRequestDto
{
    [Required]
    public Guid ClientId { get; init; }

    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string Description { get; init; } = string.Empty;

    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public ProjectStatus Status { get; init; } = ProjectStatus.Active;
}
