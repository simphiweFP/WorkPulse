using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProjectRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Project>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT p.Id,
                                  p.ClientId,
                                  c.Name AS ClientName,
                                  p.Name,
                                  p.Description,
                                  p.TotalTasks,
                                  p.StartDate,
                                  p.Status,
                                  p.CreatedAt,
                                  p.UpdatedAt,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status <> 3) AS OpenTaskCount,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status = 3) AS CompletedTaskCount
                            FROM Projects p
                            INNER JOIN Clients c ON c.Id = p.ClientId
                           ORDER BY Name
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Project>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<Project>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT p.Id,
                                  p.ClientId,
                                  c.Name AS ClientName,
                                  p.Name,
                                  p.Description,
                                  p.TotalTasks,
                                  p.StartDate,
                                  p.Status,
                                  p.CreatedAt,
                                  p.UpdatedAt,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status <> 3) AS OpenTaskCount,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status = 3) AS CompletedTaskCount
                            FROM Projects p
                            INNER JOIN Clients c ON c.Id = p.ClientId
                           WHERE p.ClientId = @ClientId
                           ORDER BY Name
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Project>(new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT p.Id,
                                  p.ClientId,
                                  c.Name AS ClientName,
                                  p.Name,
                                  p.Description,
                                  p.TotalTasks,
                                  p.StartDate,
                                  p.Status,
                                  p.CreatedAt,
                                  p.UpdatedAt,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status <> 3) AS OpenTaskCount,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.ProjectId = p.Id AND t.Status = 3) AS CompletedTaskCount
                            FROM Projects p
                            INNER JOIN Clients c ON c.Id = p.ClientId
                           WHERE p.Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Project>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM Projects WHERE Id = @Id) THEN 1 ELSE 0 END";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Projects (Id, ClientId, Name, Description, TotalTasks, StartDate, Status, CreatedAt, UpdatedAt)
                           VALUES (@Id, @ClientId, @Name, @Description, @TotalTasks, @StartDate, @Status, @CreatedAt, @UpdatedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            project.Id,
            project.ClientId,
            project.Name,
            project.Description,
            project.TotalTasks,
            project.StartDate,
            Status = (int)project.Status,
            project.CreatedAt,
            project.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Projects
                           SET ClientId = @ClientId,
                               Name = @Name,
                               Description = @Description,
                               TotalTasks = @TotalTasks,
                               StartDate = @StartDate,
                               Status = @Status,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            project.Id,
            project.ClientId,
            project.Name,
            project.Description,
            project.TotalTasks,
            project.StartDate,
            Status = (int)project.Status,
            project.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Tasks WHERE ProjectId = @Id", new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Sprints WHERE ProjectId = @Id", new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("DELETE FROM Projects WHERE Id = @Id", new { Id = id }, transaction: transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }
}
