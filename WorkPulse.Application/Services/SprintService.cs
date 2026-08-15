using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Sprints;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Application.Services;

public sealed class SprintService : ISprintService
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IClock _clock;

    public SprintService(ISprintRepository sprintRepository, IClock clock)
    {
        _sprintRepository = sprintRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<SprintDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sprints = await _sprintRepository.GetAllAsync(cancellationToken);
        return sprints.Select(Map).ToArray();
    }

    public async Task<SprintDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sprint = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        return sprint is null ? throw new NotFoundException($"Sprint '{id}' was not found.") : Map(sprint);
    }

    public async Task<SprintDto> CreateAsync(CreateSprintRequestDto request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.StartDate, request.EndDate);

        var now = _clock.UtcNow;
        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _sprintRepository.CreateAsync(sprint, cancellationToken);
        return Map(sprint);
    }

    public async Task UpdateAsync(Guid id, UpdateSprintRequestDto request, CancellationToken cancellationToken = default)
    {
        Validate(request.Name, request.StartDate, request.EndDate);

        var existing = await _sprintRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Sprint '{id}' was not found.");
        }

        await _sprintRepository.UpdateAsync(new Sprint
        {
            Id = id,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = _clock.UtcNow
        }, cancellationToken);
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
        Name = sprint.Name,
        StartDate = sprint.StartDate,
        EndDate = sprint.EndDate,
        Status = sprint.Status,
        CreatedAt = sprint.CreatedAt,
        UpdatedAt = sprint.UpdatedAt,
        TaskCount = sprint.TaskCount,
        CompletedTaskCount = sprint.CompletedTaskCount
    };

    private static void Validate(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Sprint name is required.");
        }

        if (endDate < startDate)
        {
            throw new ValidationException("Sprint end date must be on or after the start date.");
        }
    }
}
