using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkPulse.Application.DTOs.Projects;
using WorkPulse.Application.Interfaces;
using WorkPulse.Web.API.Contracts.Requests.Projects;
using WorkPulse.Web.API.Contracts.Responses.Projects;

namespace WorkPulse.Web.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProjectResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetAllAsync(cancellationToken);
        return Ok(projects.Select(Map).ToArray());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        return Ok(Map(project));
    }

    [HttpGet("/api/clients/{clientId:guid}/projects")]
    public async Task<ActionResult<IReadOnlyCollection<ProjectResponse>>> GetByClientId(Guid clientId, CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetByClientIdAsync(clientId, cancellationToken);
        return Ok(projects.Select(Map).ToArray());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var created = await _projectService.CreateAsync(new CreateProjectRequestDto
        {
            ClientId = request.ClientId,
            Name = request.Name,
            Description = request.Description,
            TotalTasks = request.TotalTasks,
            StartDate = request.StartDate,
            Status = request.Status
        }, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, Map(created));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        await _projectService.UpdateAsync(id, new UpdateProjectRequestDto
        {
            ClientId = request.ClientId,
            Name = request.Name,
            Description = request.Description,
            TotalTasks = request.TotalTasks,
            StartDate = request.StartDate,
            Status = request.Status
        }, cancellationToken);

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _projectService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static ProjectResponse Map(ProjectDto project) => new()
    {
        Id = project.Id,
        ClientId = project.ClientId,
        ClientName = project.ClientName,
        Name = project.Name,
        Description = project.Description,
        TotalTasks = project.TotalTasks,
        StartDate = project.StartDate,
        Status = project.Status,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt,
        OpenTaskCount = project.OpenTaskCount,
        CompletedTaskCount = project.CompletedTaskCount
    };
}