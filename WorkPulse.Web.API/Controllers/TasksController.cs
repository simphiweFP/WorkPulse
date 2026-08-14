using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Application.Interfaces;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;
using WorkPulse.Web.API.Contracts.Requests.Tasks;
using WorkPulse.Web.API.Contracts.Responses.Tasks;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyCollection<TaskResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok((await _taskService.GetAllAsync(cancellationToken)).Select(Map).ToArray());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(Map(await _taskService.GetByIdAsync(id, cancellationToken)));

    [HttpGet("/api/tasks/my")]
    public async Task<ActionResult<IReadOnlyCollection<TaskResponse>>> GetMy([FromQuery] GetMyTasksFilterDto filter, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _taskService.GetMyTasksAsync(userId, filter, cancellationToken);
        return Ok(items.Select(Map).ToArray());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(new CreateTaskRequestDto
        {
            ProjectId = request.ProjectId,
            AssignedUserId = request.AssignedUserId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = request.Status,
            Priority = request.Priority
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        await _taskService.UpdateAsync(id, new UpdateTaskRequestDto
        {
            ProjectId = request.ProjectId,
            AssignedUserId = request.AssignedUserId,
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Status = request.Status,
            Priority = request.Priority
        }, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignTaskRequest request, CancellationToken cancellationToken)
    {
        await _taskService.AssignAsync(id, new AssignTaskRequestDto { UserId = request.UserId }, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] TaskStatus status, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        await _taskService.UpdateStatusAsync(id, userId, isAdmin, status, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        await _taskService.CompleteAsync(id, userId, isAdmin, cancellationToken);
        return NoContent();
    }

    private static TaskResponse Map(TaskDto task) => new()
    {
        Id = task.Id,
        ProjectId = task.ProjectId,
        ProjectName = task.ProjectName,
        ClientId = task.ClientId,
        ClientName = task.ClientName,
        AssignedUserId = task.AssignedUserId,
        AssignedUserName = task.AssignedUserName,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        Status = task.Status,
        Priority = task.Priority,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CompletedAt = task.CompletedAt
    };
}