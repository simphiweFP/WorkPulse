using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Web.API.Contracts.Requests.Projects;

public sealed class CreateProjectRequest
{
    [Required]
    public Guid ClientId { get; init; }

    [Required]
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    [Required]
    public ProjectStatus Status { get; init; } = ProjectStatus.Active;
}