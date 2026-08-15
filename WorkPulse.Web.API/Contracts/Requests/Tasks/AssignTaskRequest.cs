using System.ComponentModel.DataAnnotations;

namespace WorkPulse.Web.API.Contracts.Requests.Tasks;

public sealed class AssignTaskRequest
{
    [Required]
    public string AssignedToUserId { get; init; } = string.Empty;
}