using System.ComponentModel.DataAnnotations;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Web.API.Contracts.Requests.Tasks;

public sealed class UpdateTaskStatusRequest
{
    [Required]
    public TaskStatus Status { get; init; }
}
