using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Application.DTOs.Users;
using WorkPulse.Application.Interfaces;
using WorkPulse.Application.Services;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Web.API.Tests.UseCases;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectClosedProject()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var projects = new Dictionary<Guid, Project>
        {
            [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")] = new Project
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ClientId = Guid.NewGuid(),
                Name = "Closed Project",
                Description = "Closed",
                Status = ProjectStatus.Completed,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var service = CreateService(projects: projects);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateTaskRequestDto
        {
            ProjectId = projects.Keys.Single(),
            Title = "New task",
            Description = "Task",
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        }));

        Assert.Equal("Closed projects cannot receive new tasks.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowBacklogTaskWithoutSprint()
    {
        var projectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var sprintId = (Guid?)null;
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = Guid.NewGuid(),
                Name = "Active Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc)
            }
        };

        var service = CreateService(projects: projects);

        var created = await service.CreateAsync(new CreateTaskRequestDto
        {
            ProjectId = projectId,
            SprintId = sprintId,
            Title = "Backlog task",
            Description = "No sprint yet",
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        });

        Assert.Null(created.SprintId);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectCompletedSprint()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var projectId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var sprintId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = Guid.NewGuid(),
                Name = "Active Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var sprints = new Dictionary<Guid, Sprint>
        {
            [sprintId] = new Sprint
            {
                Id = sprintId,
                Name = "Closed Sprint",
                StartDate = now.AddDays(-14),
                EndDate = now.AddDays(-7),
                Status = SprintStatus.Completed,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var service = CreateService(projects: projects, sprints: sprints);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateTaskRequestDto
        {
            ProjectId = projectId,
            SprintId = sprintId,
            Title = "Task",
            Description = "Task in closed sprint",
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        }));

        Assert.Equal("Completed sprints cannot accept new tasks.", exception.Message);
    }

    [Fact]
    public async Task AssignAsync_ShouldRejectCompletedTask()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var taskId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var tasks = new Dictionary<Guid, TaskItem>
        {
            [taskId] = new TaskItem
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Done",
                Description = "Completed task",
                Priority = TaskPriority.High,
                Status = TaskStatus.Completed,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var users = new Dictionary<string, ApplicationUser>
        {
            ["dev-1"] = new ApplicationUser { Id = "dev-1", Email = "dev@example.com", UserName = "dev@example.com", FirstName = "Dev", LastName = "One" }
        };

        var service = CreateService(tasks: tasks, users: users);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.AssignAsync(taskId, new AssignTaskRequestDto { UserId = "dev-1" }));

        Assert.Equal("Completed tasks cannot be reassigned.", exception.Message);
    }

    [Fact]
    public async Task UpdateStatus_ShouldRejectInvalidTransitionFromCompletedToTodo()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var taskId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var tasks = new Dictionary<Guid, TaskItem>
        {
            [taskId] = new TaskItem
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Done",
                Description = "Completed task",
                Priority = TaskPriority.High,
                Status = TaskStatus.Completed,
                CompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
                AssignedToUserId = "dev-1"
            }
        };

        var service = CreateService(tasks: tasks);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateStatusAsync(taskId, "dev-1", isAdmin: false, TaskStatus.Todo));

        Assert.Equal("Completed tasks cannot be reopened.", exception.Message);
    }

    private static TaskService CreateService(Dictionary<Guid, TaskItem>? tasks = null, Dictionary<Guid, Project>? projects = null, Dictionary<Guid, Sprint>? sprints = null, Dictionary<string, ApplicationUser>? users = null)
        => new(
            new FakeTaskRepository(tasks ?? new()),
            new FakeProjectRepository(projects ?? new()),
            new FakeSprintRepository(sprints ?? new()),
            new FakeUserRepository(users ?? new()),
            new FixedClock(new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc)));

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, Project> _projects;
        public FakeProjectRepository(Dictionary<Guid, Project> projects) => _projects = projects;
        public Task<IReadOnlyCollection<Project>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Project>>(_projects.Values.ToArray());
        public Task<IReadOnlyCollection<Project>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Project>>(_projects.Values.Where(p => p.ClientId == clientId).ToArray());
        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_projects.TryGetValue(id, out var project) ? project : null);
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_projects.ContainsKey(id));
        public Task CreateAsync(Project project, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Project project, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeSprintRepository : ISprintRepository
    {
        private readonly Dictionary<Guid, Sprint> _sprints;

        public FakeSprintRepository(Dictionary<Guid, Sprint> sprints) => _sprints = sprints;

        public Task<IReadOnlyCollection<Sprint>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Sprint>>(_sprints.Values.ToArray());
        public Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_sprints.TryGetValue(id, out var sprint) ? sprint : null);
        public Task CreateAsync(Sprint sprint, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly Dictionary<Guid, TaskItem> _tasks;
        public FakeTaskRepository(Dictionary<Guid, TaskItem> tasks) => _tasks = tasks;
        public Task<IReadOnlyCollection<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.ToArray());
        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_tasks.TryGetValue(id, out var task) ? task : null);
        public Task<IReadOnlyCollection<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.Where(t => t.ProjectId == projectId).ToArray());
        public Task<IReadOnlyCollection<TaskItem>> GetMyTasksAsync(string userId, TaskStatus? status, TaskPriority? priority, Guid? projectId, DateTime? dueDate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.Where(t => t.AssignedToUserId == userId).ToArray());
        public Task CreateAsync(TaskItem taskItem, CancellationToken cancellationToken = default) { _tasks[taskItem.Id] = taskItem; return Task.CompletedTask; }
        public Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default) { _tasks[taskItem.Id] = taskItem; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) { _tasks.Remove(id); return Task.CompletedTask; }
        public Task AssignAsync(Guid taskId, string? userId, CancellationToken cancellationToken = default)
        {
            if (_tasks.TryGetValue(taskId, out var task)) { task.AssignedToUserId = userId; }
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, ApplicationUser> _users;
        public FakeUserRepository(Dictionary<string, ApplicationUser> users) => _users = users;

        public Task<IReadOnlyCollection<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<ApplicationUser>>(_users.Values.ToArray());

        public Task<IReadOnlyCollection<DeveloperDto>> GetDevelopersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<DeveloperDto>>(
                _users.Values.Select(user => new DeveloperDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? string.Empty
                }).ToArray());

        public Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);
        public Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_users.Values.FirstOrDefault(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_users.Values.Any(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task CreateAsync(ApplicationUser user, string passwordHash, IEnumerable<string> roles, CancellationToken cancellationToken = default) { _users[user.Id] = user; return Task.CompletedTask; }
        public Task<IReadOnlyCollection<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<string>>(["Developer"]);
        public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(_users.ContainsKey(userId));
    }
}