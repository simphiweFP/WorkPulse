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
                           SELECT Id, ClientId, Name, Description, Status, StartDate AS CreatedAt, EndDate AS UpdatedAt
                           FROM Projects
                           ORDER BY Name
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Project>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<IReadOnlyCollection<Project>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, ClientId, Name, Description, Status, StartDate AS CreatedAt, EndDate AS UpdatedAt
                           FROM Projects
                           WHERE ClientId = @ClientId
                           ORDER BY Name
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<Project>(new CommandDefinition(sql, new { ClientId = clientId }, cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, ClientId, Name, Description, Status, StartDate AS CreatedAt, EndDate AS UpdatedAt
                           FROM Projects
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Project>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(Project project, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Projects (Id, ClientId, Name, Description, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                           VALUES (@Id, @ClientId, @Name, @Description, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            project.Id,
            project.ClientId,
            project.Name,
            project.Description,
            StartDate = project.CreatedAt,
            EndDate = (DateTime?)null,
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
            Status = (int)project.Status,
            project.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Projects WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }
}
