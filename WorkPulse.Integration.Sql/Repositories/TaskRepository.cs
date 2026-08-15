using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private const string TaskSelectColumns = "t.Id, t.ProjectId, t.SprintId, COALESCE(s.Name, '') AS SprintName, p.Name AS ProjectName, c.Id AS ClientId, c.Name AS ClientName, t.AssignedUserId AS AssignedToUserId, COALESCE(CONCAT(u.FirstName, ' ', u.LastName), '') AS AssignedUserName, t.Title, t.Description, t.DueDate AS Deadline, t.Status, t.Priority, t.CreatedAt, t.UpdatedAt, t.CompletedAt";

    public TaskRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks t
                           INNER JOIN Projects p ON p.Id = t.ProjectId
                           INNER JOIN Clients c ON c.Id = p.ClientId
                            LEFT JOIN Sprints s ON s.Id = t.SprintId
                           LEFT JOIN Users u ON u.Id = t.AssignedUserId
                           ORDER BY t.CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToArray();
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks t
                           INNER JOIN Projects p ON p.Id = t.ProjectId
                           INNER JOIN Clients c ON c.Id = p.ClientId
                            LEFT JOIN Sprints s ON s.Id = t.SprintId
                           LEFT JOIN Users u ON u.Id = t.AssignedUserId
                           WHERE t.Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<TaskItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks t
                           INNER JOIN Projects p ON p.Id = t.ProjectId
                           INNER JOIN Clients c ON c.Id = p.ClientId
                            LEFT JOIN Sprints s ON s.Id = t.SprintId
                           LEFT JOIN Users u ON u.Id = t.AssignedUserId
                           WHERE t.ProjectId = @ProjectId
                           ORDER BY t.CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));
        return items.ToArray();
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetMyTasksAsync(string userId, TaskStatus? status, TaskPriority? priority, Guid? projectId, DateTime? deadline, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                  SELECT {TaskSelectColumns}
                  FROM Tasks t
                  INNER JOIN Projects p ON p.Id = t.ProjectId
                  INNER JOIN Clients c ON c.Id = p.ClientId
                  LEFT JOIN Sprints s ON s.Id = t.SprintId
                  LEFT JOIN Users u ON u.Id = t.AssignedUserId
                  WHERE t.AssignedUserId = @UserId
                  """;

        if (status.HasValue)
        {
            sql += " AND t.Status = @Status";
        }

        if (priority.HasValue)
        {
            sql += " AND t.Priority = @Priority";
        }

        if (projectId.HasValue)
        {
            sql += " AND t.ProjectId = @ProjectId";
        }

        if (deadline.HasValue)
        {
            sql += " AND CAST(t.DueDate AS date) = CAST(@Deadline AS date)";
        }

        sql += " ORDER BY t.DueDate";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, new
        {
            UserId = userId,
            Status = (int?)status,
            Priority = (int?)priority,
            ProjectId = projectId,
            Deadline = deadline
        }, cancellationToken: cancellationToken));

        return items.ToArray();
    }

    public async Task CreateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Tasks (Id, ProjectId, SprintId, AssignedUserId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt)
                           VALUES (@Id, @ProjectId, @SprintId, @AssignedUserId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            taskItem.Id,
            taskItem.ProjectId,
            SprintId = taskItem.SprintId,
            AssignedUserId = taskItem.AssignedToUserId,
            taskItem.Title,
            taskItem.Description,
            DueDate = taskItem.Deadline,
            Status = (int)taskItem.Status,
            Priority = (int)taskItem.Priority,
            taskItem.CreatedAt,
            taskItem.UpdatedAt,
            taskItem.CompletedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Tasks
                           SET ProjectId = @ProjectId,
                               SprintId = @SprintId,
                               AssignedUserId = @AssignedUserId,
                               Title = @Title,
                               Description = @Description,
                               DueDate = @DueDate,
                               Status = @Status,
                               Priority = @Priority,
                               UpdatedAt = @UpdatedAt,
                               CompletedAt = @CompletedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            taskItem.Id,
            taskItem.ProjectId,
            SprintId = taskItem.SprintId,
            AssignedUserId = taskItem.AssignedToUserId,
            taskItem.Title,
            taskItem.Description,
            DueDate = taskItem.Deadline,
            Status = (int)taskItem.Status,
            Priority = (int)taskItem.Priority,
            taskItem.UpdatedAt,
            taskItem.CompletedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Tasks WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task AssignAsync(Guid taskId, string? userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Tasks
                           SET AssignedUserId = @UserId,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @TaskId
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { TaskId = taskId, UserId = userId, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }
}
