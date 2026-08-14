using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Today;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Services;

namespace WorkPulse.Application.Services;

public sealed class TodayService : ITodayService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ITodayTaskService _todayTaskService;
    private readonly IClock _clock;

    public TodayService(ITaskRepository taskRepository, IProjectRepository projectRepository, IClientRepository clientRepository, ITodayTaskService todayTaskService, IClock clock)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _clientRepository = clientRepository;
        _todayTaskService = todayTaskService;
        _clock = clock;
    }

    public async Task<TodayDashboardDto> GetMyTodayAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetMyTasksAsync(userId, null, null, null, null, cancellationToken);
        return await BuildAsync(tasks, cancellationToken);
    }

    public async Task<TodayDashboardDto> GetAdminTodayAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        return await BuildAsync(tasks, cancellationToken);
    }

    private async Task<TodayDashboardDto> BuildAsync(IReadOnlyCollection<TaskItem> tasks, CancellationToken cancellationToken)
    {
        var projects = (await _projectRepository.GetAllAsync(cancellationToken)).ToDictionary(project => project.Id);
        var clients = (await _clientRepository.GetAllAsync(cancellationToken)).ToDictionary(client => client.Id);
        var now = _clock.UtcNow;

        var candidates = tasks.Select(task => MapCandidate(task, projects, clients)).ToArray();
        var ranked = _todayTaskService.RankDeveloperTasks(candidates, now);
        var summary = _todayTaskService.BuildSummary(candidates, now);

        return new TodayDashboardDto
        {
            Summary = new TodaySummaryDto
            {
                Total = summary.Total,
                Overdue = summary.Overdue,
                DueToday = summary.DueToday,
                HighPriority = summary.HighPriority
            },
            Tasks = ranked.Select(MapTask).ToArray()
        };
    }

    private static TodayTaskCandidate MapCandidate(TaskItem task, IReadOnlyDictionary<Guid, Domain.Entities.Project> projects, IReadOnlyDictionary<Guid, Domain.Entities.Client> clients)
    {
        if (!projects.TryGetValue(task.ProjectId, out var project))
        {
            throw new NotFoundException($"Project '{task.ProjectId}' was not found.");
        }

        if (!clients.TryGetValue(project.ClientId, out var client))
        {
            throw new NotFoundException($"Client '{project.ClientId}' was not found.");
        }

        return new TodayTaskCandidate
        {
            Id = task.Id,
            Title = task.Title,
            ClientName = client.Name,
            ProjectName = project.Name,
            Priority = task.Priority,
            DueDate = task.Deadline,
            Status = task.Status
        };
    }

    private static TodayTaskDto MapTask(TodayTaskCandidate candidate) => new()
    {
        Id = candidate.Id,
        Title = candidate.Title,
        ClientName = candidate.ClientName,
        ProjectName = candidate.ProjectName,
        Priority = candidate.Priority,
        DueDate = candidate.DueDate,
        Status = candidate.Status,
        RecommendationReason = candidate.RecommendationReason,
        Score = candidate.Score
    };
}
