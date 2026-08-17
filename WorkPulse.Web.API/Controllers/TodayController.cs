using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.DTOs.Today;
using WorkPulse.Application.Interfaces;
using WorkPulse.Web.API.Contracts.Responses.Today;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TodayController : ControllerBase
{
    private readonly ITodayService _todayService;

    public TodayController(ITodayService todayService)
    {
        _todayService = todayService;
    }

    [HttpGet]
    public async Task<ActionResult<TodayDashboardResponse>> GetMyToday(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var dashboard = await _todayService.GetMyTodayAsync(userId, cancellationToken);
        return Ok(Map(dashboard));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TodayDashboardResponse>> GetAdminToday(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var dashboard = await _todayService.GetAdminTodayAsync(userId, cancellationToken);
        return Ok(Map(dashboard));
    }

    private static TodayDashboardResponse Map(TodayDashboardDto dashboard) => new()
    {
        Summary = new TodaySummaryResponse
        {
            Total = dashboard.Summary.Total,
            Overdue = dashboard.Summary.Overdue,
            DeadlineToday = dashboard.Summary.DeadlineToday,
            HighPriority = dashboard.Summary.HighPriority
        },
        TopPriority = MapTask(dashboard.TopPriority),
        Overdue = dashboard.Overdue.Select(MapTask).ToArray(),
        DueToday = dashboard.DueToday.Select(MapTask).ToArray(),
        RecommendedNext = dashboard.RecommendedNext.Select(MapTask).ToArray(),
        CompletedToday = dashboard.CompletedToday.Select(MapTask).ToArray(),
        SprintWorkComplete = dashboard.SprintWorkComplete,
        SprintName = dashboard.SprintName,
        SprintCompletedTasks = dashboard.SprintCompletedTasks,
        SprintTotalTasks = dashboard.SprintTotalTasks,
        SprintCompletedPoints = dashboard.SprintCompletedPoints,
        SprintTotalPoints = dashboard.SprintTotalPoints
    };

    private static TodayTaskResponse MapTask(TodayTaskDto task) => new()
    {
        Id = task.Id,
        ProjectId = task.ProjectId,
        ProjectName = task.ProjectName,
        ClientId = task.ClientId,
        ClientName = task.ClientName,
        Type = task.Type,
        Title = task.Title,
        Description = task.Description,
        SprintOrder = task.SprintOrder,
        Deadline = task.Deadline,
        Status = task.Status,
        Priority = task.Priority,
        RecommendationReason = task.RecommendationReason,
        Score = task.Score
    };
}
