using System;

namespace WorkPulse.Application.DTOs.Users;

public sealed class UserManagementDto
{
    public string Id { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsPending => string.Equals(Role, "Pending", StringComparison.OrdinalIgnoreCase);
}