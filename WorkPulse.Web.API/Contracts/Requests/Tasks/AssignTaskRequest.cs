using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Web.API.Contracts.Requests.Tasks;

public sealed class AssignTaskRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;
}