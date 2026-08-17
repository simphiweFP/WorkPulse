using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Sprints;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.Services;

public sealed class SprintService : ISprintService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IClock _clock;

    public SprintService(IProjectRepository projectRepository, ISprintRepository sprintRepository, IClock clock)
    {
        _projectRepository = projectRepository;
        _sprintRepository = sprintRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<SprintDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        return sprints.Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<SprintDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sprints = await _sprintRepository.GetByProjectIdAsync(projectId, cancellationToken);
        return sprints.Select(Map).ToArray();
    }

    public async Task<SprintDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        return sprint is null ? throw new NotFoundException($"Sprint '{id}' was not found.") : Map(sprint);
    }

    public async Task<SprintDto> CreateAsync(CreateSprintRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateProjectAsync(request.ProjectId, cancellationToken);
        Validate(request.Name, request.StartDate, request.EndDate, request.TotalTasks);
        ValidateRequestedStatus(request.Status, isCreate: true);

        var now = _clock.UtcNow;
        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now,
            TotalTasks = request.TotalTasks
        };

        await _sprintRepository.CreateAsync(sprint, cancellationToken);
        await _sprintRepository.RecalculateStatusAsync(sprint.Id, cancellationToken);

        var created = await _sprintRepository.GetByIdAsync(sprint.Id, cancellationToken)
            ?? throw new NotFoundException($"Sprint '{sprint.Id}' was not found.");

        return Map(created);
    }

    public async Task UpdateAsync(Guid id, UpdateSprintRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateProjectAsync(request.ProjectId, cancellationToken);
        Validate(request.Name, request.StartDate, request.EndDate, request.TotalTasks);
        ValidateRequestedStatus(request.Status, isCreate: false);

        var existing = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Sprint '{id}' was not found.");
        }

        await _sprintRepository.UpdateAsync(new Sprint
        {
            Id = id,
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = _clock.UtcNow,
            TotalTasks = request.TotalTasks
        }, cancellationToken);

        await _sprintRepository.RecalculateStatusAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Sprint '{id}' was not found.");
        }

        await _sprintRepository.DeleteAsync(id, cancellationToken);
    }

    private static SprintDto Map(Sprint sprint) => new()
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
        CompletedTaskCount = sprint.CompletedTaskCount,
        TotalPoints = sprint.TotalPoints,
        CompletedPoints = sprint.CompletedPoints
    };

    private static void Validate(string name, DateTime startDate, DateTime endDate, int totalTasks)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Sprint name is required.");
        }

        if (endDate < startDate)
        {
            throw new ValidationException("Sprint end date must be on or after the start date.");
        }

        if (totalTasks < 0)
        {
            throw new ValidationException("Expected task count cannot be negative.");
        }
    }

    private async Task ValidateProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{projectId}' was not found.");
        }
    }

    private static void ValidateRequestedStatus(SprintStatus requestedStatus, bool isCreate)
    {
        if (requestedStatus == SprintStatus.Completed)
        {
            if (isCreate)
            {
                throw new ValidationException("New sprints cannot be created as completed.");
            }

            throw new ValidationException("Sprints are completed automatically when all story points are completed.");
        }
    }
}
