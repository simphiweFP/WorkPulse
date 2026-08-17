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

    public async Task<IReadOnlyCollection<TaskDto>> GetBacklogAsync(string userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var items = await _taskRepository.GetAllAsync(cancellationToken);
        if (!isAdmin)
        {
            var userProjects = (await _taskRepository.GetMyTasksAsync(userId, null, null, null, null, cancellationToken))
                .Select(task => task.ProjectId)
                .Distinct()
                .ToHashSet();

            items = items.Where(task => userProjects.Contains(task.ProjectId)).ToArray();
        }

        return items
            .Where(task => task.Status != TaskStatus.Completed && !task.SprintId.HasValue)
            .Select(Map)
            .ToArray();
    }

    public async Task<TaskDto> CreateAsync(CreateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEnums(request.Status, request.Priority);
        ValidateStoryPoints(request.StoryPoints);

        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        if (project.ClientId != request.ClientId)
        {
            throw new ValidationException("The selected project does not belong to the selected client.");
        }

        if (!project.CanAcceptNewTasks())
        {
            throw new ValidationException("Closed projects cannot receive new tasks.");
        }

        var sprint = await ValidateSprintAsync(request.ProjectId, request.SprintId, cancellationToken);
        await ValidateSprintCapacityAsync(sprint, request.StoryPoints, cancellationToken);
        var sprintOrder = await ResolveSprintOrderAsync(sprint, request.SprintOrder, cancellationToken);
        var assignedToUserId = await ValidateAssignedUserAsync(request.AssignedToUserId, cancellationToken);

        var now = _clock.UtcNow;
        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            StoryPoints = request.StoryPoints,
            SprintOrder = sprintOrder,
            Deadline = request.Deadline,
            Status = request.Status,
            Priority = request.Priority,
            AssignedToUserId = assignedToUserId,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = null
        };

        await _taskRepository.CreateAsync(entity, cancellationToken);
        await RecalculateSprintStatusAsync(entity.SprintId, cancellationToken);
        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateTaskRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEnums(request.Status, request.Priority);
        ValidateStoryPoints(request.StoryPoints);

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

        if (project.ClientId != request.ClientId)
        {
            throw new ValidationException("The selected project does not belong to the selected client.");
        }

        if (!project.CanAcceptNewTasks())
        {
            throw new ValidationException("Closed projects cannot receive new tasks.");
        }

        var sprint = await ValidateSprintAsync(request.ProjectId, request.SprintId, cancellationToken);
        if (request.SprintId != existing.SprintId)
        {
            await ValidateSprintCapacityAsync(sprint, request.StoryPoints, cancellationToken);
        }
        else if (request.SprintId.HasValue)
        {
            await ValidateSprintCapacityAsync(sprint, request.StoryPoints, cancellationToken, id);
        }

        var sprintOrder = await ResolveSprintOrderAsync(sprint, request.SprintOrder, cancellationToken, existing);
        var assignedToUserId = string.IsNullOrWhiteSpace(request.AssignedToUserId)
            ? existing.AssignedToUserId
            : await ValidateAssignedUserAsync(request.AssignedToUserId, cancellationToken);

        await _taskRepository.UpdateAsync(new TaskItem
        {
            Id = id,
            ProjectId = request.ProjectId,
            SprintId = request.SprintId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            StoryPoints = request.StoryPoints,
            SprintOrder = sprintOrder,
            Deadline = request.Deadline,
            Status = request.Status,
            Priority = request.Priority,
            AssignedToUserId = assignedToUserId,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = _clock.UtcNow,
            CompletedAt = request.Status == TaskStatus.Completed ? existing.CompletedAt ?? _clock.UtcNow : existing.CompletedAt,
            ProjectName = existing.ProjectName,
            ClientId = existing.ClientId,
            ClientName = existing.ClientName,
            AssignedUserName = existing.AssignedUserName
        }, cancellationToken);

        await RecalculateSprintStatusAsync(existing.SprintId, cancellationToken);
        if (existing.SprintId != request.SprintId)
        {
            await RecalculateSprintStatusAsync(request.SprintId, cancellationToken);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (exists is null)
        {
            throw new NotFoundException($"Task '{id}' was not found.");
        }

        await _taskRepository.DeleteAsync(id, cancellationToken);
        await RecalculateSprintStatusAsync(exists.SprintId, cancellationToken);
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
        if (!roles.Contains("Developer") && !roles.Contains("Admin"))
        {
            throw new ValidationException("Task can only be assigned to a Developer or Admin.");
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
        await RecalculateSprintStatusAsync(task.SprintId, cancellationToken);
    }

    public async Task CompleteAsync(Guid id, string currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        await UpdateStatusAsync(id, currentUserId, isAdmin, TaskStatus.Completed, cancellationToken);
    }

    private async Task RecalculateSprintStatusAsync(Guid? sprintId, CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return;
        }

        await _sprintRepository.RecalculateStatusAsync(sprintId.Value, cancellationToken);
    }

    private static TaskDto Map(TaskItem task) => new()
    {
        Id = task.Id,
        ProjectId = task.ProjectId,
        SprintId = task.SprintId,
        Type = task.Type,
        SprintName = string.IsNullOrWhiteSpace(task.SprintName) ? (task.SprintId.HasValue ? string.Empty : "Backlog") : task.SprintName,
        ProjectName = task.ProjectName,
        Title = task.Title,
        Description = task.Description,
        Deadline = task.Deadline,
        Status = task.Status,
        Priority = task.Priority,
        StoryPoints = task.StoryPoints,
        SprintOrder = task.SprintOrder,
        AssignedToUserId = task.AssignedToUserId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CompletedAt = task.CompletedAt,
        ClientId = task.ClientId,
        ClientName = task.ClientName,
        AssignedUserName = task.AssignedUserName,
        RecommendationReason = task.IsCompleted ? string.Empty : task.Title
    };

    private async Task<Sprint?> ValidateSprintAsync(Guid projectId, Guid? sprintId, CancellationToken cancellationToken)
    {
        if (!sprintId.HasValue)
        {
            return null;
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

        if (sprint.ProjectId != projectId)
        {
            throw new ValidationException("The selected sprint does not belong to the selected project.");
        }

        return sprint;
    }

    private async Task ValidateSprintCapacityAsync(Sprint? sprint, int storyPoints, CancellationToken cancellationToken, Guid? taskIdToExclude = null)
    {
        if (sprint is null)
        {
            return;
        }

        var tasks = await _taskRepository.GetBySprintIdAsync(sprint.Id, cancellationToken);
        var currentPoints = tasks.Where(task => taskIdToExclude is null || task.Id != taskIdToExclude.Value).Sum(task => task.StoryPoints);
        var nextTotal = currentPoints + storyPoints;

        if (nextTotal > sprint.TotalTasks)
        {
            throw new ValidationException("The story points assigned to this task exceed the sprint point capacity.");
        }
    }

    private async Task<string?> ValidateAssignedUserAsync(string? assignedToUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(assignedToUserId))
        {
            return null;
        }

        var normalizedUserId = assignedToUserId.Trim();
        var user = await _userRepository.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User '{normalizedUserId}' was not found.");
        }

        var roles = await _userRepository.GetRolesAsync(normalizedUserId, cancellationToken);
        if (!roles.Contains("Developer") && !roles.Contains("Admin"))
        {
            throw new ValidationException("Task can only be assigned to a Developer or Admin.");
        }

        return normalizedUserId;
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

    private static void ValidateStoryPoints(int storyPoints)
    {
        if (storyPoints < 1)
        {
            throw new ValidationException("Story points must be at least 1.");
        }
    }

    private async Task<int?> ResolveSprintOrderAsync(Sprint? sprint, int? requestedSprintOrder, CancellationToken cancellationToken, TaskItem? existing = null)
    {
        if (sprint is null)
        {
            return null;
        }

        if (requestedSprintOrder.HasValue)
        {
            if (requestedSprintOrder.Value < 1)
            {
                throw new ValidationException("Sprint order must be at least 1.");
            }

            return requestedSprintOrder;
        }

        if (existing is not null && existing.SprintId == sprint.Id && existing.SprintOrder.HasValue)
        {
            return existing.SprintOrder;
        }

        return await GetNextSprintOrderAsync(sprint.Id, cancellationToken, existing?.Id);
    }

    private async Task<int> GetNextSprintOrderAsync(Guid sprintId, CancellationToken cancellationToken, Guid? taskIdToExclude = null)
    {
        var tasks = await _taskRepository.GetBySprintIdAsync(sprintId, cancellationToken);
        var maxOrder = tasks
            .Where(task => taskIdToExclude is null || task.Id != taskIdToExclude.Value)
            .Select(task => task.SprintOrder)
            .Where(order => order.HasValue)
            .Select(order => order!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return maxOrder + 1;
    }
}