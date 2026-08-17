using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Projects;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClock _clock;

    public ProjectService(IProjectRepository projectRepository, IClientRepository clientRepository, IClock clock)
    {
        _projectRepository = projectRepository;
        _clientRepository = clientRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        return projects.Select(project => Map(project, project.ClientName)).ToArray();
    }

    public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetByIdAsync(id, cancellationToken);
        return project is null ? throw new NotFoundException($"Project '{id}' was not found.") : Map(project, project.ClientName);
    }

    public async Task<IReadOnlyCollection<ProjectDto>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var projects = await _projectRepository.GetByClientIdAsync(clientId, cancellationToken);
        return projects.Select(project => Map(project, project.ClientName)).ToArray();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateStatus(request.Status);

        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException($"Client '{request.ClientId}' was not found.");
        }

        var now = _clock.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = request.ClientId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            TotalTasks = request.TotalTasks,
            StartDate = request.StartDate,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _projectRepository.CreateAsync(project, cancellationToken);
        return Map(project, client.Name);
    }

    public async Task UpdateAsync(Guid id, UpdateProjectRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateStatus(request.Status);

        var existing = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Project '{id}' was not found.");
        }

        var client = await _clientRepository.GetByIdAsync(request.ClientId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException($"Client '{request.ClientId}' was not found.");
        }

        await _projectRepository.UpdateAsync(new Project
        {
            Id = id,
            ClientId = request.ClientId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            TotalTasks = request.TotalTasks,
            StartDate = request.StartDate,
            Status = request.Status,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = _clock.UtcNow,
            OpenTaskCount = existing.OpenTaskCount,
            CompletedTaskCount = existing.CompletedTaskCount
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _projectRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Project '{id}' was not found.");
        }

        await _projectRepository.DeleteAsync(id, cancellationToken);
    }

    private static ProjectDto Map(Project project, string clientName = "") => new()
    {
        Id = project.Id,
        ClientId = project.ClientId,
        ClientName = clientName,
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

    private static void ValidateStatus(WorkPulse.Domain.Enums.ProjectStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ValidationException("Invalid project status.");
        }
    }
}