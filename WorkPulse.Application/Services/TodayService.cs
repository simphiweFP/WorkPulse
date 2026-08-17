using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Today;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using WorkPulse.Domain.Services;
using DomainTaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.Services;

public sealed class TodayService : ITodayService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ISprintRepository _sprintRepository;
    private readonly ITodayTaskService _todayTaskService;
    private readonly IClock _clock;

    public TodayService(ITaskRepository taskRepository, IProjectRepository projectRepository, IClientRepository clientRepository, ISprintRepository sprintRepository, ITodayTaskService todayTaskService, IClock clock)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _clientRepository = clientRepository;
        _sprintRepository = sprintRepository;
        _todayTaskService = todayTaskService;
        _clock = clock;
    }

    public async Task<TodayDashboardDto> GetMyTodayAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetMyTasksAsync(userId, null, null, null, null, cancellationToken);
        return await BuildAsync(tasks, cancellationToken);
    }

    public async Task<TodayDashboardDto> GetAdminTodayAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetMyTasksAsync(userId, null, null, null, null, cancellationToken);
        return await BuildAsync(tasks, cancellationToken);
    }

    private async Task<TodayDashboardDto> BuildAsync(IReadOnlyCollection<TaskItem> tasks, CancellationToken cancellationToken)
    {
        var projects = (await _projectRepository.GetAllAsync(cancellationToken)).ToDictionary(project => project.Id);
        var clients = (await _clientRepository.GetAllAsync(cancellationToken)).ToDictionary(client => client.Id);
        var sprints = (await _sprintRepository.GetAllAsync(cancellationToken)).ToDictionary(sprint => sprint.Id);
        var now = _clock.UtcNow;

        var candidates = tasks.Select(task => MapCandidate(task, projects, clients, sprints)).ToArray();
        var activeSprintCandidates = candidates.Where(task => IsInActiveSprint(task, now)).ToArray();
        var currentSprintName = activeSprintCandidates.Select(task => task.SprintName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty;
        var sprintCompletedTasks = activeSprintCandidates.Count(task => task.Status == DomainTaskStatus.Completed);
        var sprintTotalTasks = activeSprintCandidates.Length;
        var sprintCompletedPoints = activeSprintCandidates.Where(task => task.Status == DomainTaskStatus.Completed).Sum(task => task.StoryPoints);
        var sprintTotalPoints = activeSprintCandidates.Sum(task => task.StoryPoints);
        var sprintWorkComplete = activeSprintCandidates.Length > 0 && activeSprintCandidates.All(task => task.Status == DomainTaskStatus.Completed);

        if (sprintWorkComplete)
        {
            return new TodayDashboardDto
            {
                Summary = new TodaySummaryDto(),
                SprintWorkComplete = true,
                SprintName = currentSprintName,
                SprintCompletedTasks = sprintCompletedTasks,
                SprintTotalTasks = sprintTotalTasks,
                SprintCompletedPoints = sprintCompletedPoints,
                SprintTotalPoints = sprintTotalPoints
            };
        }

        var sections = _todayTaskService.BuildSections(candidates, now);
        var summary = _todayTaskService.BuildSummary(candidates, now);

        return new TodayDashboardDto
        {
            Summary = new TodaySummaryDto
            {
                Total = summary.Total,
                Overdue = summary.Overdue,
                DeadlineToday = summary.DueToday,
                HighPriority = summary.HighPriority
            },
            TopPriority = MapTask(sections.TopPriority),
            Overdue = sections.Overdue.Select(MapTask).ToArray(),
            DueToday = sections.DueToday.Select(MapTask).ToArray(),
            RecommendedNext = sections.RecommendedNext.Select(MapTask).ToArray(),
            CompletedToday = sections.CompletedToday.Select(MapTask).ToArray(),
            SprintWorkComplete = false,
            SprintName = currentSprintName,
            SprintCompletedTasks = sprintCompletedTasks,
            SprintTotalTasks = sprintTotalTasks,
            SprintCompletedPoints = sprintCompletedPoints,
            SprintTotalPoints = sprintTotalPoints
        };
    }

    private static TodayTaskCandidate MapCandidate(TaskItem task, IReadOnlyDictionary<Guid, Domain.Entities.Project> projects, IReadOnlyDictionary<Guid, Domain.Entities.Client> clients, IReadOnlyDictionary<Guid, Sprint> sprints)
    {
        if (!projects.TryGetValue(task.ProjectId, out var project))
        {
            throw new NotFoundException($"Project '{task.ProjectId}' was not found.");
        }

        if (!clients.TryGetValue(project.ClientId, out var client))
        {
            throw new NotFoundException($"Client '{project.ClientId}' was not found.");
        }

        sprints.TryGetValue(task.SprintId ?? Guid.Empty, out var sprint);

        return new TodayTaskCandidate
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ClientId = client.Id,
            SprintId = task.SprintId,
            Type = task.Type,
            Title = task.Title,
            Description = task.Description,
            ClientName = client.Name,
            ProjectName = project.Name,
            Priority = task.Priority,
            SprintOrder = task.SprintOrder,
            SprintStartDate = sprint?.StartDate,
            SprintEndDate = sprint?.EndDate,
            SprintStatus = sprint?.Status,
            SprintName = sprint?.Name ?? string.Empty,
            DueDate = task.Deadline,
            StoryPoints = task.StoryPoints,
            Status = task.Status
        };
    }

    private static TodayTaskDto MapTask(TodayTaskCandidate? candidate) => candidate is null ? new TodayTaskDto() : new TodayTaskDto
    {
        Id = candidate.Id,
        ProjectId = candidate.ProjectId,
        ClientId = candidate.ClientId,
        Type = candidate.Type,
        Title = candidate.Title,
        Description = candidate.Description,
        ClientName = candidate.ClientName,
        ProjectName = candidate.ProjectName,
        Priority = candidate.Priority,
        Deadline = candidate.DueDate,
        Status = candidate.Status,
        RecommendationReason = candidate.RecommendationReason,
        Score = candidate.Score
    };

    private static bool IsInActiveSprint(TodayTaskCandidate task, DateTime utcNow)
        => task.SprintId.HasValue
           && (task.SprintStatus == SprintStatus.Active
               || (task.SprintStartDate.HasValue && task.SprintStartDate.Value.Date <= utcNow.Date));
}
