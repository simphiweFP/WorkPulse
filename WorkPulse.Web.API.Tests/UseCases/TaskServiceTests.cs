using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Sprints;
using WorkPulse.Application.DTOs.Tasks;
using WorkPulse.Application.DTOs.Users;
using WorkPulse.Application.Interfaces;
using WorkPulse.Application.Services;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using WorkPulse.Integration.Identity.Roles;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Web.API.Tests.UseCases;

public class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldRejectClosedProject()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var clientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var projects = new Dictionary<Guid, Project>
        {
            [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")] = new Project
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ClientId = clientId,
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
            ClientId = clientId,
            ProjectId = projects.Keys.Single(),
            Title = "New task",
            Description = "Task",
            StoryPoints = 1,
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
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = clientId,
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
            ClientId = clientId,
            ProjectId = projectId,
            SprintId = sprintId,
            Title = "Backlog task",
            Description = "No sprint yet",
            StoryPoints = 1,
            SprintOrder = null,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        });

        Assert.Null(created.SprintId);
    }

    [Fact]
    public async Task GetBacklogAsync_ShouldExcludeSprintAssignedTasks()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var projectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var sprintId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        var tasks = new Dictionary<Guid, TaskItem>
        {
            [Guid.Parse("11111111-1111-1111-1111-111111111111")] = new TaskItem
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ProjectId = projectId,
                SprintId = null,
                Title = "Backlog item",
                Description = "Unscheduled work",
                StoryPoints = 3,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now,
                ProjectName = "Project",
                ClientId = clientId,
                ClientName = "Client",
                AssignedUserName = "Dev One"
            },
            [Guid.Parse("22222222-2222-2222-2222-222222222222")] = new TaskItem
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProjectId = projectId,
                SprintId = sprintId,
                Title = "Sprint item",
                Description = "Committed work",
                StoryPoints = 5,
                SprintOrder = 1,
                Priority = TaskPriority.High,
                Status = TaskStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now,
                ProjectName = "Project",
                ClientId = clientId,
                ClientName = "Client",
                AssignedUserName = "Dev One"
            }
        };

        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = clientId,
                Name = "Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var service = CreateService(tasks: tasks, projects: projects);

        var backlog = await service.GetBacklogAsync("dev-1", isAdmin: true);

        Assert.Single(backlog);
        Assert.Equal("Backlog item", backlog.Single().Title);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectCompletedSprint()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var projectId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var sprintId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var clientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = clientId,
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
            ClientId = clientId,
            ProjectId = projectId,
            SprintId = sprintId,
            Title = "Task",
            Description = "Task in closed sprint",
            StoryPoints = 1,
            SprintOrder = 1,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        }));

        Assert.Equal("Completed sprints cannot accept new tasks.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectMismatchedClientAndProject()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var projectId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var projectClientId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var requestClientId = Guid.Parse("ffffffff-eeee-dddd-cccc-bbbbbbbbbbbb");
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = projectClientId,
                Name = "Active Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var service = CreateService(projects: projects);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateTaskRequestDto
        {
            ClientId = requestClientId,
            ProjectId = projectId,
            Title = "Task",
            Description = "Client mismatch",
            StoryPoints = 1,
            SprintOrder = 1,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        }));

        Assert.Equal("The selected project does not belong to the selected client.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectMismatchedClientAndProject()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var taskId = Guid.Parse("abababab-abab-abab-abab-abababababab");
        var projectId = Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd");
        var projectClientId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var requestClientId = Guid.Parse("21098765-4321-4321-4321-210987654321");

        var tasks = new Dictionary<Guid, TaskItem>
        {
            [taskId] = new TaskItem
            {
                Id = taskId,
                ProjectId = projectId,
                SprintId = null,
                Title = "Open",
                Description = "Open task",
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now,
                ProjectName = "Project",
                ClientId = projectClientId,
                ClientName = "Client",
                AssignedUserName = "Dev One"
            }
        };

        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = projectClientId,
                Name = "Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var service = CreateService(tasks: tasks, projects: projects);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(taskId, new UpdateTaskRequestDto
        {
            ClientId = requestClientId,
            ProjectId = projectId,
            Title = "Updated",
            Description = "Updated",
            StoryPoints = 1,
            SprintOrder = 1,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        }));

        Assert.Equal("The selected project does not belong to the selected client.", exception.Message);
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

    [Fact]
    public async Task UpdateAsync_ShouldRejectUnknownAssignedUserId()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var taskId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var projectId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var clientId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var tasks = new Dictionary<Guid, TaskItem>
        {
            [taskId] = new TaskItem
            {
                Id = taskId,
                ProjectId = projectId,
                Title = "Task",
                Description = "Task",
                StoryPoints = 1,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                AssignedToUserId = "dev-1",
                CreatedAt = now,
                UpdatedAt = now,
                ProjectName = "Project",
                ClientId = clientId,
                ClientName = "Client",
                AssignedUserName = "Dev One"
            }
        };

        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = clientId,
                Name = "Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var users = new Dictionary<string, ApplicationUser>
        {
            ["dev-1"] = new ApplicationUser { Id = "dev-1", Email = "dev@example.com", UserName = "dev@example.com", FirstName = "Dev", LastName = "One" }
        };

        var service = CreateService(tasks: tasks, projects: projects, users: users);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(taskId, new UpdateTaskRequestDto
        {
            ClientId = clientId,
            ProjectId = projectId,
            Title = "Updated",
            Description = "Updated",
            StoryPoints = 1,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo,
            AssignedToUserId = "not-a-real-user"
        }));

        Assert.Equal("User 'not-a-real-user' was not found.", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPreserveExistingAssigneeWhenNotProvided()
    {
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var taskId = Guid.Parse("eeeeeeee-dddd-dddd-dddd-dddddddddddd");
        var projectId = Guid.Parse("ffffffff-eeee-eeee-eeee-ffffffffffff");
        var clientId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var tasks = new Dictionary<Guid, TaskItem>
        {
            [taskId] = new TaskItem
            {
                Id = taskId,
                ProjectId = projectId,
                Title = "Task",
                Description = "Task",
                StoryPoints = 1,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                AssignedToUserId = "dev-1",
                CreatedAt = now,
                UpdatedAt = now,
                ProjectName = "Project",
                ClientId = clientId,
                ClientName = "Client",
                AssignedUserName = "Dev One"
            }
        };

        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project
            {
                Id = projectId,
                ClientId = clientId,
                Name = "Project",
                Description = "Open",
                Status = ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        var users = new Dictionary<string, ApplicationUser>
        {
            ["dev-1"] = new ApplicationUser { Id = "dev-1", Email = "dev@example.com", UserName = "dev@example.com", FirstName = "Dev", LastName = "One" }
        };

        var service = CreateService(tasks: tasks, projects: projects, users: users);

        await service.UpdateAsync(taskId, new UpdateTaskRequestDto
        {
            ClientId = clientId,
            ProjectId = projectId,
            Title = "Updated",
            Description = "Updated",
            StoryPoints = 1,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        });

        Assert.Equal("dev-1", tasks[taskId].AssignedToUserId);
    }

    [Fact]
    public async Task FullWorkflow_CreateAssignComplete_UpdatesTaskState()
    {
        // Arrange
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        var clientId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var sprintId = Guid.NewGuid();

        var users = new Dictionary<string, ApplicationUser>
        {
            ["dev-1"] = new ApplicationUser { Id = "dev-1", Email = "dev@example.com", UserName = "dev@example.com", FirstName = "Dev", LastName = "One", }
        };
        var userRoles = new Dictionary<string, string>
        {
            ["dev-1"] = WorkPulseRoles.Developer
        };
        var projects = new Dictionary<Guid, Project>
        {
            [projectId] = new Project { Id = projectId, ClientId = clientId, Name = "Proj", Description = "Test", Status = ProjectStatus.Active, CreatedAt = now, UpdatedAt = now }
        };

        var sprints = new Dictionary<Guid, Sprint>
        {
            [sprintId] = new Sprint { Id = sprintId, ProjectId = projectId, Name = "Sprint 1", StartDate = now, EndDate = now.AddDays(7), Status = SprintStatus.Active, CreatedAt = now, UpdatedAt = now,
                TotalTasks = 8
            }
        };

        var tasks = new Dictionary<Guid, TaskItem>();

        var service = CreateService(tasks: tasks, projects: projects, sprints: sprints, users: users, userRoles: userRoles);

        // Act
        var created = await service.CreateAsync(new CreateTaskRequestDto
        {
            ClientId = clientId,
            ProjectId = projectId,
            SprintId = sprintId,
            Title = "Flow Task",
            Description = "End-to-end",
            StoryPoints = 3,
            Priority = TaskPriority.Medium,
            Status = TaskStatus.Todo
        });

        await service.AssignAsync(created.Id, new AssignTaskRequestDto { UserId = "dev-1" });
        await service.UpdateStatusAsync(created.Id, "dev-1", isAdmin: true, TaskStatus.InProgress);
        await service.CompleteAsync(created.Id, "dev-1", isAdmin: true);

        // Assert
        Assert.True(tasks.ContainsKey(created.Id));
        var stored = tasks[created.Id];
        Assert.Equal("dev-1", stored.AssignedToUserId);
        Assert.Equal(TaskStatus.Completed, stored.Status);
        Assert.NotNull(stored.CompletedAt);
    }

    private static TaskService CreateService(Dictionary<Guid, TaskItem>? tasks = null, Dictionary<Guid, Project>? projects = null, Dictionary<Guid, Sprint>? sprints = null, Dictionary<string, ApplicationUser>? users = null, Dictionary<string, string>? userRoles = null)
        => new(
            new FakeTaskRepository(tasks ?? new()),
            new FakeProjectRepository(projects ?? new()),
            new FakeSprintRepository(sprints ?? new()),
        new FakeUserRepository(users ?? new(), userRoles),
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
        public Task<IReadOnlyCollection<Sprint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Sprint>>(_sprints.Values.Where(sprint => sprint.ProjectId == projectId).ToArray());
        public Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_sprints.TryGetValue(id, out var sprint) ? sprint : null);
        public Task<SprintProgressDto> GetProgressAsync(Guid sprintId, CancellationToken cancellationToken = default) => Task.FromResult(new SprintProgressDto());
        public Task CreateAsync(Sprint sprint, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RecalculateStatusAsync(Guid sprintId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private readonly Dictionary<Guid, TaskItem> _tasks;
        public FakeTaskRepository(Dictionary<Guid, TaskItem> tasks) => _tasks = tasks;
        public Task<IReadOnlyCollection<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.ToArray());
        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_tasks.TryGetValue(id, out var task) ? task : null);
        public Task<IReadOnlyCollection<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.Where(t => t.ProjectId == projectId).ToArray());
        public Task<IReadOnlyCollection<TaskItem>> GetBySprintIdAsync(Guid sprintId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TaskItem>>(_tasks.Values.Where(t => t.SprintId == sprintId).ToArray());
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
        private readonly Dictionary<string, List<string>> _roles = new();
        public FakeUserRepository(
            Dictionary<string, ApplicationUser> users,
            Dictionary<string, string>? userRoles = null)
        {
            _users = users;

            if (userRoles is null)
            {
                return;
            }

            foreach (var (userId, role) in userRoles)
            {
                _roles[userId] = new List<string> { role };
            }
        }
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
        public Task<IReadOnlyCollection<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (_roles.TryGetValue(userId, out var roles))
            {
                return Task.FromResult<IReadOnlyCollection<string>>(roles.ToArray());
            }

            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        public Task<IReadOnlyCollection<UserManagementDto>> GetUserManagementAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<UserManagementDto>>(
                _users.Values.Select(user => new UserManagementDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? string.Empty,
                    CreatedAt = user.CreatedAt,
                    Role = _roles.TryGetValue(user.Id, out var r) && r.Count > 0 ? r[0] : "Pending"
                }).ToArray());

        public Task<int> CountAdminsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_roles.Values.Count(list => list.Any(role => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))));

        public Task UpdateRoleAsync(string userId, string? role, CancellationToken cancellationToken = default)
        {
            _roles[userId] = new List<string>();
            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                _roles[userId].Add(role.Trim());
            }

            return Task.CompletedTask;
        }
        public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(_users.ContainsKey(userId));
    }
}