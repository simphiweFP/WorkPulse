using WorkPulse.Application.DTOs.Dashboard;
using WorkPulse.Application.Interfaces;
using TaskPriority = WorkPulse.Domain.Enums.TaskPriority;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Application.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClock _clock;

    public AdminDashboardService(IDashboardRepository dashboardRepository, IClientRepository clientRepository, IProjectRepository projectRepository, ITaskRepository taskRepository, IUserRepository userRepository, IClock clock)
    {
        _dashboardRepository = dashboardRepository;
        _clientRepository = clientRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _clock = clock;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.GetAllAsync(cancellationToken);
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        var tasks = await _taskRepository.GetAllAsync(cancellationToken);
        var developers = await _userRepository.GetDevelopersAsync(cancellationToken);
        var utcNow = _clock.UtcNow;
        var todayStart = utcNow.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var overdue = await _dashboardRepository.GetOverdueCountAsync(utcNow, cancellationToken);
        var dueToday = await _dashboardRepository.GetDueTodayCountAsync(todayStart, tomorrowStart, cancellationToken);
        var completed = await _dashboardRepository.GetCompletedCountAsync(cancellationToken);
        var inProgress = tasks.Count(task => task.Status == TaskStatus.InProgress);
        var critical = tasks.Count(task => task.Priority == TaskPriority.Critical);
        var high = tasks.Count(task => task.Priority == TaskPriority.High);
        var medium = tasks.Count(task => task.Priority == TaskPriority.Medium);
        var low = tasks.Count(task => task.Priority == TaskPriority.Low);

        return new AdminDashboardDto
        {
            Summary = new AdminDashboardSummaryDto
            {
                Clients = clients.Count,
                Projects = projects.Count,
                Tasks = tasks.Count,
                TeamMembers = developers.Count
            },
            TaskOverview = new AdminDashboardTaskOverviewDto
            {
                Overdue = overdue,
                DueToday = dueToday,
                InProgress = inProgress,
                Completed = completed
            },
            PriorityBreakdown = new AdminDashboardPriorityBreakdownDto
            {
                Critical = critical,
                High = high,
                Medium = medium,
                Low = low
            },
            RecentTasks = tasks
                .OrderByDescending(task => task.CreatedAt)
                .Take(8)
                .Select(task => new AdminDashboardRecentTaskDto
                {
                    Id = task.Id,
                    Title = task.Title,
                    ProjectName = task.ProjectName,
                    ClientName = task.ClientName,
                    AssigneeName = task.AssignedUserName,
                    Priority = task.Priority,
                    Deadline = task.Deadline,
                    Status = task.Status
                })
                .ToArray()
        };
    }
}
