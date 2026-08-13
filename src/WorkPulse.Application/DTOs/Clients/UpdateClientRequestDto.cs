using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Application.DTOs.Clients;

public sealed class UpdateClientRequestDto
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Phone { get; init; } = string.Empty;

    [Required]
    public string Address { get; init; } = string.Empty;
}
