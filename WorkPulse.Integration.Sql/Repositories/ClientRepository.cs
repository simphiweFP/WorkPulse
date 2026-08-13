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
                           SELECT Id, Name, Email AS ContactEmail, Phone AS PhoneNumber, Address AS Description, CreatedAt, UpdatedAt,
                                  Name AS ContactName
                           FROM Clients
                           ORDER BY CreatedAt DESC
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var clients = await connection.QueryAsync<Client>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return clients.ToArray();
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, Name, Email AS ContactEmail, Phone AS PhoneNumber, Address AS Description, CreatedAt, UpdatedAt,
                                  Name AS ContactName
                           FROM Clients
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Client>(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           INSERT INTO Clients (Id, Name, Email, Phone, Address, CreatedAt, UpdatedAt)
                           VALUES (@Id, @Name, @Email, @Phone, @Address, @CreatedAt, @UpdatedAt)
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.Id,
            client.Name,
            Email = client.ContactEmail,
            Phone = client.PhoneNumber,
            Address = client.Description,
            client.CreatedAt,
            client.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           UPDATE Clients
                           SET Name = @Name,
                               Email = @Email,
                               Phone = @Phone,
                               Address = @Address,
                               UpdatedAt = @UpdatedAt
                           WHERE Id = @Id
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            client.Id,
            client.Name,
            Email = client.ContactEmail,
            Phone = client.PhoneNumber,
            Address = client.Description,
            client.UpdatedAt
        }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Clients WHERE Id = @Id";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Clients WHERE Email = @Email";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        return count > 0;
    }
}
