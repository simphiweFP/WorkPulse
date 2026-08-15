using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using WorkPulse.Integration.Sql.Repositories;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Tests.Repositories;

public class TaskRepositoryTests
{
    [Fact]
    public async Task CreateAsync_ThenGetByIdAsync_ShouldReturnJoinedTaskWithSprint()
    {
        var databaseName = $"WorkPulseTaskRepo_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        try
        {
            await CreateSchemaAsync(databaseName);

            var connectionString = ConnectionString(databaseName);
            var repository = new TaskRepository(new TestConnectionFactory(connectionString));

            var sprintId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            var userId = "dev-1";
            var taskId = Guid.NewGuid();

            await SeedReferenceDataAsync(connectionString, clientId, projectId, sprintId, userId);

            var task = new TaskItem
            {
                Id = taskId,
                ProjectId = projectId,
                SprintId = sprintId,
                AssignedToUserId = userId,
                Title = "Repository task",
                Description = "Repository task description",
                Deadline = new DateTime(2026, 8, 14, 17, 30, 0, DateTimeKind.Utc),
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.High,
                CreatedAt = new DateTime(2026, 8, 14, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 14, 8, 15, 0, DateTimeKind.Utc)
            };

            await repository.CreateAsync(task);

            var persisted = await repository.GetByIdAsync(taskId);

            Assert.NotNull(persisted);
            Assert.Equal(taskId, persisted!.Id);
            Assert.Equal(sprintId, persisted.SprintId);
            Assert.Equal("Dev One", persisted.AssignedUserName);
            Assert.Equal("Project Alpha", persisted.ProjectName);
            Assert.Equal("Client A", persisted.ClientName);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task GetMyTasksAsync_ShouldFilterByUserStatusPriorityProjectAndDeadline()
    {
        var databaseName = $"WorkPulseTaskRepoFilter_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        try
        {
            await CreateSchemaAsync(databaseName);

            var connectionString = ConnectionString(databaseName);
            var repository = new TaskRepository(new TestConnectionFactory(connectionString));

            var clientId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var otherProjectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();
            var userId = "dev-1";

            await SeedReferenceDataAsync(connectionString, clientId, projectId, sprintId, userId, otherProjectId);

            await InsertTaskAsync(connectionString, new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                SprintId = sprintId,
                AssignedToUserId = userId,
                Title = "Matching task",
                Description = "Matches all filters",
                Deadline = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Critical,
                CreatedAt = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 19, 10, 30, 0, DateTimeKind.Utc)
            });

            await InsertTaskAsync(connectionString, new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                SprintId = sprintId,
                AssignedToUserId = userId,
                Title = "Wrong priority",
                Description = "Should be filtered out",
                Deadline = new DateTime(2026, 8, 20, 13, 0, 0, DateTimeKind.Utc),
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.High,
                CreatedAt = new DateTime(2026, 8, 19, 11, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 19, 11, 30, 0, DateTimeKind.Utc)
            });

            await InsertTaskAsync(connectionString, new TaskItem
            {
                Id = Guid.NewGuid(),
                ProjectId = otherProjectId,
                SprintId = sprintId,
                AssignedToUserId = userId,
                Title = "Wrong project",
                Description = "Should be filtered out",
                Deadline = new DateTime(2026, 8, 20, 9, 0, 0, DateTimeKind.Utc),
                Status = TaskStatus.InProgress,
                Priority = TaskPriority.Critical,
                CreatedAt = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 8, 19, 12, 30, 0, DateTimeKind.Utc)
            });

            var items = await repository.GetMyTasksAsync(
                userId,
                TaskStatus.InProgress,
                TaskPriority.Critical,
                projectId,
                new DateTime(2026, 8, 20, 23, 59, 59, DateTimeKind.Utc));

            var task = Assert.Single(items);
            Assert.Equal("Matching task", task.Title);
            Assert.Equal(sprintId, task.SprintId);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private static async Task SeedReferenceDataAsync(string connectionString, Guid clientId, Guid projectId, Guid sprintId, string assignedUserId, Guid? otherProjectId = null)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(new CommandDefinition("""
                                                             INSERT INTO Clients (Id, Name, ContactName, ContactEmail, PhoneNumber, Description, CreatedAt, UpdatedAt, IsDeleted)
                                                             VALUES (@Id, @Name, @ContactName, @ContactEmail, @PhoneNumber, @Description, @CreatedAt, @UpdatedAt, 0)
                                                             """, new
        {
            Id = clientId,
            Name = "Client A",
            ContactName = "Contact A",
            ContactEmail = "client.a@workpulse.local",
            PhoneNumber = "555-1000",
            Description = "Client A description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }));

        await connection.ExecuteAsync(new CommandDefinition("""
                                                             INSERT INTO Projects (Id, ClientId, Name, Description, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                                                             VALUES (@Id, @ClientId, @Name, @Description, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                                                             """, new
        {
            Id = projectId,
            ClientId = clientId,
            Name = "Project Alpha",
            Description = "Project Alpha description",
            StartDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = (int)ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }));

        if (otherProjectId.HasValue)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                                                                 INSERT INTO Projects (Id, ClientId, Name, Description, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                                                                 VALUES (@Id, @ClientId, @Name, @Description, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                                                                 """, new
            {
                Id = otherProjectId.Value,
                ClientId = clientId,
                Name = "Project Beta",
                Description = "Project Beta description",
                StartDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = (int)ProjectStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }));
        }

        await connection.ExecuteAsync(new CommandDefinition("""
                                                             INSERT INTO Sprints (Id, Name, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                                                             VALUES (@Id, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                                                             """, new
        {
            Id = sprintId,
            Name = "Sprint 1",
            StartDate = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            Status = (int)SprintStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }));

        await connection.ExecuteAsync(new CommandDefinition("""
                                                             INSERT INTO Users (Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt, IsDeleted)
                                                             VALUES (@Id, @FirstName, @LastName, @Email, @UserName, @PasswordHash, @CreatedAt, @UpdatedAt, 0)
                                                             """, new
        {
            Id = assignedUserId,
            FirstName = "Dev",
            LastName = "One",
            Email = "dev.one@workpulse.local",
            UserName = "dev.one@workpulse.local",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }));
    }

    private static async Task InsertTaskAsync(string connectionString, TaskItem task)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(new CommandDefinition("""
                                                             INSERT INTO Tasks (Id, ProjectId, SprintId, AssignedUserId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt)
                                                             VALUES (@Id, @ProjectId, @SprintId, @AssignedUserId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)
                                                             """, new
        {
            task.Id,
            task.ProjectId,
            SprintId = task.SprintId,
            AssignedUserId = task.AssignedToUserId,
            task.Title,
            task.Description,
            DueDate = task.Deadline,
            Status = (int)task.Status,
            Priority = (int)task.Priority,
            task.CreatedAt,
            task.UpdatedAt,
            task.CompletedAt
        }));
    }

    private static async Task CreateDatabaseAsync(string databaseName)
    {
        await using var connection = await OpenMasterConnectionAsync();
        await connection.ExecuteAsync($"CREATE DATABASE [{databaseName}]");
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        await using var connection = await OpenMasterConnectionAsync();
        var sql = $"""
                   IF DB_ID('{databaseName}') IS NOT NULL
                   BEGIN
                       ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                       DROP DATABASE [{databaseName}];
                   END
                   """;
        await connection.ExecuteAsync(sql);
    }

    private static async Task CreateSchemaAsync(string databaseName)
    {
        await using var connection = await OpenDatabaseConnectionAsync(databaseName);
        const string sql = """
                           CREATE TABLE dbo.Clients (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               Name NVARCHAR(200) NOT NULL,
                               ContactName NVARCHAR(200) NOT NULL,
                               ContactEmail NVARCHAR(256) NOT NULL,
                               PhoneNumber NVARCHAR(50) NOT NULL,
                               Description NVARCHAR(1000) NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               IsDeleted BIT NOT NULL DEFAULT 0
                           );

                           CREATE TABLE dbo.Projects (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               ClientId UNIQUEIDENTIFIER NOT NULL,
                               Name NVARCHAR(200) NOT NULL,
                               Description NVARCHAR(2000) NOT NULL,
                               StartDate DATETIME2 NOT NULL,
                               EndDate DATETIME2 NULL,
                               Status INT NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL
                           );

                           CREATE TABLE dbo.Sprints (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               Name NVARCHAR(200) NOT NULL,
                               StartDate DATETIME2 NOT NULL,
                               EndDate DATETIME2 NOT NULL,
                               Status INT NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL
                           );

                           CREATE TABLE dbo.Users (
                               Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                               FirstName NVARCHAR(100) NOT NULL,
                               LastName NVARCHAR(100) NOT NULL,
                               Email NVARCHAR(256) NOT NULL,
                               UserName NVARCHAR(256) NOT NULL,
                               PasswordHash NVARCHAR(MAX) NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               IsDeleted BIT NOT NULL DEFAULT 0
                           );

                           CREATE TABLE dbo.Tasks (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               ProjectId UNIQUEIDENTIFIER NOT NULL,
                               SprintId UNIQUEIDENTIFIER NULL,
                               AssignedUserId NVARCHAR(64) NULL,
                               Title NVARCHAR(200) NOT NULL,
                               Description NVARCHAR(2000) NOT NULL,
                               DueDate DATETIME2 NULL,
                               Status INT NOT NULL,
                               Priority INT NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               CompletedAt DATETIME2 NULL
                           );
                           """;
        await connection.ExecuteAsync(sql);
    }

    private static string ConnectionString(string databaseName)
        => $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    private static async Task<SqlConnection> OpenMasterConnectionAsync()
    {
        var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<SqlConnection> OpenDatabaseConnectionAsync(string databaseName)
    {
        var connection = new SqlConnection(ConnectionString(databaseName));
        await connection.OpenAsync();
        return connection;
    }

    private sealed class TestConnectionFactory : WorkPulse.Application.Interfaces.IDbConnectionFactory
    {
        private readonly string _connectionString;

        public TestConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
