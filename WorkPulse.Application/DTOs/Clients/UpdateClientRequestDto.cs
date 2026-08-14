using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Application.DTOs.Clients;

public sealed class UpdateClientRequestDto
{
    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string ContactName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ContactEmail { get; init; } = string.Empty;

    [Required]
    public string PhoneNumber { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}