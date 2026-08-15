using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT c.Id,
                                  c.Name,
                                  c.ContactName,
                                  c.ContactEmail,
                                  c.PhoneNumber,
                                  c.Description,
                                  c.CreatedAt,
                                  c.UpdatedAt,
                                  c.IsDeleted,
                                  (SELECT COUNT(1) FROM Projects p WHERE p.ClientId = c.Id) AS ProjectCount,
                                  (SELECT COUNT(1)
                                   FROM Tasks t
                                   INNER JOIN Projects p2 ON p2.Id = t.ProjectId
                                   WHERE p2.ClientId = c.Id AND t.Status <> 3) AS OpenTaskCount
                           FROM Clients c
                           WHERE c.IsDeleted = 0
                           ORDER BY c.CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var clients = await connection.QueryAsync<Client>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return clients.ToArray();
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT c.Id,
                                  c.Name,
                                  c.ContactName,
                                  c.ContactEmail,
                                  c.PhoneNumber,
                                  c.Description,
                                  c.CreatedAt,
                                  c.UpdatedAt,
                                  c.IsDeleted,
                                  (SELECT COUNT(1) FROM Projects p WHERE p.ClientId = c.Id) AS ProjectCount,
                                  (SELECT COUNT(1)
                                   FROM Tasks t
                                   INNER JOIN Projects p2 ON p2.Id = t.ProjectId
                                   WHERE p2.ClientId = c.Id AND t.Status <> 3) AS OpenTaskCount
                           FROM Clients c
                           WHERE c.Id = @Id AND c.IsDeleted = 0
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Client>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Clients WHERE Id = @Id AND IsDeleted = 0";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Clients (Id, Name, ContactName, ContactEmail, PhoneNumber, Description, CreatedAt, UpdatedAt, IsDeleted)
                           VALUES (@Id, @Name, @ContactName, @ContactEmail, @PhoneNumber, @Description, @CreatedAt, @UpdatedAt, @IsDeleted)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.Id,
            client.Name,
            client.ContactName,
            client.ContactEmail,
            client.PhoneNumber,
            client.Description,
            client.CreatedAt,
            client.UpdatedAt,
            client.IsDeleted
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Clients
                           SET Name = @Name,
                               ContactName = @ContactName,
                               ContactEmail = @ContactEmail,
                               PhoneNumber = @PhoneNumber,
                               Description = @Description,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.Id,
            client.Name,
            client.ContactName,
            client.ContactEmail,
            client.PhoneNumber,
            client.Description,
            client.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Clients SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, UpdatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Clients WHERE ContactEmail = @Email AND IsDeleted = 0";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        return count > 0;
    }
}
