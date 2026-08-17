using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.DTOs.Sprints;
using WorkPulse.Application.Interfaces;
using WorkPulse.Web.API.Contracts.Requests.Sprints;
using WorkPulse.Web.API.Contracts.Responses.Sprints;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;

    public SprintsController(ISprintService sprintService)
    {
        _sprintService = sprintService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SprintResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var sprints = await _sprintService.GetAllAsync(cancellationToken);
        return Ok(sprints.Select(Map).ToArray());
    }

    [HttpGet("project/{projectId:guid}")]
    public async Task<ActionResult<IReadOnlyCollection<SprintResponse>>> GetByProjectId(Guid projectId, CancellationToken cancellationToken)
    {
        var sprints = await _sprintService.GetByProjectIdAsync(projectId, cancellationToken);
        return Ok(sprints.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SprintResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sprint = await _sprintService.GetByIdAsync(id, cancellationToken);
        return Ok(Map(sprint));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SprintResponse>> Create([FromBody] CreateSprintRequest request, CancellationToken cancellationToken)
    {
        var created = await _sprintService.CreateAsync(new CreateSprintRequestDto
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            TotalTasks = request.TotalTasks
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SprintResponse>> Update(Guid id, [FromBody] UpdateSprintRequest request, CancellationToken cancellationToken)
    {
        await _sprintService.UpdateAsync(id, new UpdateSprintRequestDto
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            TotalTasks = request.TotalTasks
        }, cancellationToken);

        var updated = await _sprintService.GetByIdAsync(id, cancellationToken);
        return Ok(Map(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sprintService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static SprintResponse Map(SprintDto sprint) => new()
    {
        Id = sprint.Id,
        ProjectId = sprint.ProjectId,
        Name = sprint.Name,
        StartDate = sprint.StartDate,
        EndDate = sprint.EndDate,
        Status = sprint.Status,
        CreatedAt = sprint.CreatedAt,
        UpdatedAt = sprint.UpdatedAt,
        TotalTasks = sprint.TotalTasks,
        TaskCount = sprint.TaskCount,
        CompletedTaskCount = sprint.CompletedTaskCount
    };
}
