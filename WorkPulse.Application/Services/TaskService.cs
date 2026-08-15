using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;

    public TaskService(ITaskRepository taskRepository, IProjectRepository projectRepository, ISprintRepository sprintRepository, IUserRepository userRepository, IClock clock)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _sprintRepository = sprintRepository;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<TaskDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _taskRepository.GetAllAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<TaskDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task is null ? throw new NotFoundException($"Task '{id}' was not found.") : Map(task);
    }

    public async Task<IReadOnlyCollection<TaskDto>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => (await _taskRepository.GetByProjectIdAsync(projectId, cancellationToken)).Select(Map).ToArray();

    public async Task<IReadOnlyCollection<TaskDto>> GetMyTasksAsync(string userId, GetMyTasksFilterDto filter, CancellationToken cancellationToken = default)
    {
        var items = await _taskRepository.GetMyTasksAsync(userId, filter.Status, filter.Priority, filter.ProjectId, filter.Deadline, cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEnums(request.Status, request.Priority);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        if (!project.CanAcceptNewTasks())
        {
            throw new ValidationException("Closed projects cannot receive new tasks.");
        }

        await ValidateSprintAsync(request.SprintId, cancellationToken);

        var now = _clock.UtcNow;
        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            Status = request.Status,
            Priority = request.Priority,
            AssignedToUserId = request.AssignedToUserId,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = null
        };

        await _taskRepository.CreateAsync(entity, cancellationToken);
        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEnums(request.Status, request.Priority);

        var existing = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            throw new NotFoundException($"Task '{id}' was not found.");
        }

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        if (!project.CanAcceptNewTasks())
        {
            throw new ValidationException("Closed projects cannot receive new tasks.");
        }

        await ValidateSprintAsync(request.SprintId, cancellationToken);

        await _taskRepository.UpdateAsync(new TaskItem
        {
            Id = id,
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            Status = request.Status,
            Priority = request.Priority,
            AssignedToUserId = request.AssignedToUserId,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = _clock.UtcNow,
            CompletedAt = request.Status == TaskStatus.Completed ? existing.CompletedAt ?? _clock.UtcNow : existing.CompletedAt,
            ProjectName = existing.ProjectName,
            ClientId = existing.ClientId,
            ClientName = existing.ClientName,
            AssignedUserName = existing.AssignedUserName
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (exists is null)
        {
            throw new NotFoundException($"Task '{id}' was not found.");
        }

        await _taskRepository.DeleteAsync(id, cancellationToken);
    }

    public async Task AssignAsync(Guid id, AssignTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException($"Task '{id}' was not found.");
        if (task.IsCompleted)
        {
            throw new ValidationException("Completed tasks cannot be reassigned.");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User '{request.UserId}' was not found.");
        }

        var roles = await _userRepository.GetRolesAsync(request.UserId, cancellationToken);
        if (!roles.Contains("Developer"))
        {
            throw new ValidationException("Task can only be assigned to a Developer.");
        }

        try
        {
            task.AssignTo(request.UserId, _clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _taskRepository.AssignAsync(id, request.UserId, cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, string currentUserId, bool isAdmin, TaskStatus status, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException($"Task '{id}' was not found.");

        if (!isAdmin && !string.Equals(task.AssignedToUserId, currentUserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("You can only modify your own assigned tasks.");
        }

        if (task.Status == TaskStatus.Completed && status != TaskStatus.Completed)
        {
            throw new ValidationException("Completed tasks cannot be reopened.");
        }

        if (task.Status == TaskStatus.Todo && status == TaskStatus.InProgress)
        {
            task.Status = status;
        }
        else if (task.Status == TaskStatus.Todo && status == TaskStatus.Completed)
        {
            task.Status = status;
            task.CompletedAt = _clock.UtcNow;
        }
        else if (task.Status == TaskStatus.InProgress && status == TaskStatus.Completed)
        {
            task.Status = status;
            task.CompletedAt = _clock.UtcNow;
        }
        else if (task.Status != status)
        {
            throw new ValidationException("Invalid task status transition.");
        }

        task.UpdatedAt = _clock.UtcNow;
        await _taskRepository.UpdateAsync(task, cancellationToken);
    }

    public async Task CompleteAsync(Guid id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await UpdateStatusAsync(id, currentUserId, isAdmin, TaskStatus.Completed, cancellationToken);
    }

    private static TaskDto Map(TaskItem task) => new()
    {
        Id = task.Id,
        ProjectId = task.ProjectId,
        SprintId = task.SprintId,
        SprintName = string.IsNullOrWhiteSpace(task.SprintName) ? (task.SprintId.HasValue ? string.Empty : "Backlog") : task.SprintName,
        ProjectName = task.ProjectName,
        Title = task.Title,
        Description = task.Description,
        Deadline = task.Deadline,
        Status = task.Status,
        Priority = task.Priority,
        AssignedToUserId = task.AssignedToUserId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CompletedAt = task.CompletedAt,
        ClientId = task.ClientId,
        ClientName = task.ClientName,
        AssignedUserName = task.AssignedUserName,
        RecommendationReason = task.IsCompleted ? string.Empty : task.Title
    };

    private async Task ValidateSprintAsync(Guid? sprintId, CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return;
        }

        var sprint = await _sprintRepository.GetByIdAsync(sprintId.Value, cancellationToken);
        if (sprint is null)
        {
            throw new NotFoundException($"Sprint '{sprintId}' was not found.");
        }

        if (sprint.Status == SprintStatus.Completed)
        {
            throw new ValidationException("Completed sprints cannot accept new tasks.");
        }
    }

    private static void ValidateEnums(TaskStatus status, TaskPriority priority)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ValidationException("Invalid task status.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ValidationException("Invalid task priority.");
        }
    }
}