using Dapper;
using WorkPulse.Application.DTOs.Sprints;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;
using WorkPulse.Domain.Enums;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class SprintRepository : ISprintRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SprintRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Sprint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = await BuildSelectSqlAsync(cancellationToken);

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Sprint>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<Sprint>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var sql = await BuildSelectSqlAsync(cancellationToken, "WHERE s.ProjectId = @ProjectId");

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Sprint>(new CommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sql = await BuildSelectSqlAsync(cancellationToken, "WHERE s.Id = @Id");

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Sprint>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<SprintProgressDto> GetProgressAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT
                               COUNT(1) AS TaskCount,
                               COALESCE(SUM(CASE WHEN t.Status = 3 THEN 1 ELSE 0 END), 0) AS CompletedTaskCount,
                               COALESCE(SUM(COALESCE(t.StoryPoints, 0)), 0) AS TotalPoints,
                               COALESCE(SUM(CASE WHEN t.Status = 3 THEN COALESCE(t.StoryPoints, 0) ELSE 0 END), 0) AS CompletedPoints
                           FROM Tasks t
                           WHERE t.SprintId = @SprintId
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<SprintProgressDto>(new CommandDefinition(sql, new { SprintId = sprintId }, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Sprints (Id, ProjectId, Name, StartDate, EndDate, Status, TotalTasks, CreatedAt, UpdatedAt)
                           VALUES (@Id, @ProjectId, @Name, @StartDate, @EndDate, @Status, @TotalTasks, @CreatedAt, @UpdatedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Status = (int)sprint.Status,
            sprint.TotalTasks,
            sprint.CreatedAt,
            sprint.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task RecalculateStatusAsync(Guid sprintId, CancellationToken cancellationToken = default)
    {
        var progress = await GetProgressAsync(sprintId, cancellationToken);
        var status = DetermineStatus(progress);

        const string sql = """
                           UPDATE Sprints
                           SET Status = @Status,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = sprintId,
            Status = (int)status,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Sprints
                           SET ProjectId = @ProjectId,
                               Name = @Name,
                               StartDate = @StartDate,
                               EndDate = @EndDate,
                               Status = @Status,
                               TotalTasks = @TotalTasks,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Status = (int)sprint.Status,
            sprint.TotalTasks,
            sprint.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Sprints WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    private static async Task<string> BuildSelectSqlAsync(CancellationToken cancellationToken, string? whereClause = null)
    {
        await Task.Yield();

        var sql = $$"""
                   IF COL_LENGTH('dbo.Sprints', 'ProjectId') IS NULL
                   BEGIN
                       SELECT s.Id,
                              CAST(NULL AS UNIQUEIDENTIFIER) AS ProjectId,
                              s.Name,
                              s.StartDate,
                              s.EndDate,
                              s.Status,
                              COALESCE(s.TotalTasks, 0) AS TotalTasks,
                              (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id) AS TaskCount,
                              (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3) AS CompletedTaskCount,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id), 0) AS TotalPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3), 0) AS CompletedPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id), 0) AS TotalStoryPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3), 0) AS CompletedStoryPoints,
                              s.CreatedAt,
                              s.UpdatedAt
                       FROM Sprints s
                       {{whereClause}}
                       ORDER BY s.StartDate DESC, s.Name;
                   END
                   ELSE
                   BEGIN
                       SELECT s.Id,
                              s.ProjectId,
                              s.Name,
                              s.StartDate,
                              s.EndDate,
                              s.Status,
                              COALESCE(s.TotalTasks, 0) AS TotalTasks,
                              (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id) AS TaskCount,
                              (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3) AS CompletedTaskCount,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id), 0) AS TotalPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3), 0) AS CompletedPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id), 0) AS TotalStoryPoints,
                              COALESCE((SELECT SUM(COALESCE(t.StoryPoints, 0)) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3), 0) AS CompletedStoryPoints,
                              s.CreatedAt,
                              s.UpdatedAt
                       FROM Sprints s
                       {{whereClause}}
                       ORDER BY s.StartDate DESC, s.Name;
                   END
                   """;

        return sql.Replace("{{whereClause}}", string.IsNullOrWhiteSpace(whereClause) ? string.Empty : whereClause, StringComparison.Ordinal);
    }

    private static SprintStatus DetermineStatus(SprintProgressDto progress)
    {
        if (progress.TaskCount <= 0 || progress.CompletedPoints <= 0)
        {
            return SprintStatus.Planned;
        }

        return progress.CompletedPoints >= progress.TotalPoints ? SprintStatus.Completed : SprintStatus.Active;
    }
}
