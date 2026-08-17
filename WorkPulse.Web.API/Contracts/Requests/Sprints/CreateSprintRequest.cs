using System.ComponentModel.DataAnnotations;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Web.API.Contracts.Requests.Sprints;

public sealed class CreateSprintRequest
{
    [Required]
    public Guid ProjectId { get; init; }

    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public DateTime StartDate { get; init; }

    [Required]
    public DateTime EndDate { get; init; }

    [Required]
    public SprintStatus Status { get; init; } = SprintStatus.Planned;

    [Range(0, int.MaxValue)]
    public int TotalTasks { get; init; }
}
