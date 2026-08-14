using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.DTOs.Projects;

public sealed class UpdateProjectRequestDto
{
    [Required]
    public Guid ClientId { get; init; }

    [Required]
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    [Required]
    public ProjectStatus Status { get; init; }
}