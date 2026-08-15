using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

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
        const string sql = """
                           SELECT s.Id,
                                  s.Name,
                                  s.StartDate,
                                  s.EndDate,
                                  s.Status,
                                  s.CreatedAt,
                                  s.UpdatedAt,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id) AS TaskCount,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3) AS CompletedTaskCount
                           FROM Sprints s
                           ORDER BY s.StartDate DESC, s.Name
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Sprint>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT s.Id,
                                  s.Name,
                                  s.StartDate,
                                  s.EndDate,
                                  s.Status,
                                  s.CreatedAt,
                                  s.UpdatedAt,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id) AS TaskCount,
                                  (SELECT COUNT(1) FROM Tasks t WHERE t.SprintId = s.Id AND t.Status = 3) AS CompletedTaskCount
                           FROM Sprints s
                           WHERE s.Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Sprint>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Sprints (Id, Name, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                           VALUES (@Id, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            sprint.Id,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Status = (int)sprint.Status,
            sprint.CreatedAt,
            sprint.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Sprints
                           SET Name = @Name,
                               StartDate = @StartDate,
                               EndDate = @EndDate,
                               Status = @Status,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            sprint.Id,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Status = (int)sprint.Status,
            sprint.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Sprints WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }
}
