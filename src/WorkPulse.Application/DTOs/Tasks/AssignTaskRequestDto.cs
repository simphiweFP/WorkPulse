using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Application.DTOs.Tasks;

public sealed class AssignTaskRequestDto
{
    [Required]
    public string UserId { get; init; } = string.Empty;
}
