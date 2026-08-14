using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private const string TaskSelectColumns = "Id, ProjectId, AssignedUserId AS AssignedToUserId, Title, Description, DueDate AS Deadline, Status, Priority, CreatedAt, UpdatedAt, CompletedAt";

    public TaskRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks
                           ORDER BY CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return items.ToArray();
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<TaskItem>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                           SELECT {TaskSelectColumns}
                           FROM Tasks
                           WHERE ProjectId = @ProjectId
                           ORDER BY CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));
        return items.ToArray();
    }

    public async Task<IReadOnlyCollection<TaskItem>> GetMyTasksAsync(string userId, TaskStatus? status, TaskPriority? priority, Guid? projectId, DateTime? dueDate, CancellationToken cancellationToken = default)
    {
        var sql = $"""
                  SELECT {TaskSelectColumns}
                  FROM Tasks
                  WHERE AssignedUserId = @UserId
                  """;

        if (status.HasValue)
        {
            sql += " AND Status = @Status";
        }

        if (priority.HasValue)
        {
            sql += " AND Priority = @Priority";
        }

        if (projectId.HasValue)
        {
            sql += " AND ProjectId = @ProjectId";
        }

        if (dueDate.HasValue)
        {
            sql += " AND CAST(DueDate AS date) = CAST(@DueDate AS date)";
        }

        sql += " ORDER BY DueDate";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var items = await connection.QueryAsync<TaskItem>(new CommandDefinition(sql, new
        {
            UserId = userId,
            Status = (int?)status,
            Priority = (int?)priority,
            ProjectId = projectId,
            DueDate = dueDate
        }, cancellationToken: cancellationToken));

        return items.ToArray();
    }

    public async Task CreateAsync(TaskItem taskItem, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Tasks (Id, ProjectId, AssignedUserId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt)
                           VALUES (@Id, @ProjectId, @AssignedUserId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            taskItem.Id,
            taskItem.ProjectId,
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
