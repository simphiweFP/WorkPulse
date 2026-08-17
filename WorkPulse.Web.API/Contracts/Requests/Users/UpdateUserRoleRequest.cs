using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Web.API.Contracts.Requests.Users;

public sealed class UpdateUserRoleRequest
{
    [Required]
    public string Role { get; init; } = string.Empty;
}