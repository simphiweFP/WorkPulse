using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Application.Interfaces;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;
using WorkPulse.Web.API.Contracts.Requests.Tasks;
using WorkPulse.Web.API.Contracts.Responses.Tasks;
using WorkPulse.Web.API.Contracts.Responses.Today;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITodayService _todayService;

    public TasksController(ITaskService taskService, ITodayService todayService)
    {
        _taskService = taskService;
        _todayService = todayService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyCollection<TaskResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok((await _taskService.GetAllAsync(cancellationToken)).Select(Map).ToArray());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(Map(await _taskService.GetByIdAsync(id, cancellationToken)));

    [HttpGet("my")]
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

    [HttpGet("today")]
    public async Task<ActionResult<TodayDashboardResponse>> GetToday(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var dashboard = await _todayService.GetMyTodayAsync(userId, cancellationToken);
        return Ok(new TodayDashboardResponse
        {
            Summary = new TodaySummaryResponse
            {
                Total = dashboard.Summary.Total,
                Overdue = dashboard.Summary.Overdue,
                DeadlineToday = dashboard.Summary.DeadlineToday,
                HighPriority = dashboard.Summary.HighPriority
            },
            Tasks = dashboard.Tasks.Select(task => new TodayTaskResponse
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                ProjectName = task.ProjectName,
                ClientId = task.ClientId,
                ClientName = task.ClientName,
                Title = task.Title,
                Description = task.Description,
                Deadline = task.Deadline,
                Status = task.Status,
                Priority = task.Priority,
                RecommendationReason = task.RecommendationReason,
                Score = task.Score
            }).ToArray()
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(new CreateTaskRequestDto
        {
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title,
            Description = request.Description,
            Deadline = request.Deadline,
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
            SprintId = request.SprintId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title,
            Description = request.Description,
            Deadline = request.Deadline,
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
        await _taskService.AssignAsync(id, new AssignTaskRequestDto { UserId = request.AssignedToUserId }, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        await _taskService.UpdateStatusAsync(id, userId, isAdmin, request.Status, cancellationToken);
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
        SprintId = task.SprintId,
        SprintName = task.SprintName,
        ProjectName = task.ProjectName,
        ClientId = task.ClientId,
        ClientName = task.ClientName,
        AssignedToUserId = task.AssignedToUserId,
        AssignedUserName = task.AssignedUserName,
        Title = task.Title,
        Description = task.Description,
        Deadline = task.Deadline,
        Status = task.Status,
        Priority = task.Priority,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CompletedAt = task.CompletedAt,
        RecommendationReason = task.RecommendationReason
    };
}