using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt
                           FROM Users
                           WHERE Id = @UserId
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt
                           FROM Users
                           WHERE Email = @Email
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationUser>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task CreateAsync(ApplicationUser user, string passwordHash, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        const string insertUserSql = """
                                     INSERT INTO Users (Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt, IsDeleted)
                                     VALUES (@Id, @FirstName, @LastName, @Email, @UserName, @PasswordHash, @CreatedAt, @UpdatedAt, 0)
                                     """;

        const string getRoleSql = "SELECT Id FROM Roles WHERE Name = @Name";
        const string insertRoleSql = "INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(insertUserSql, new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                UserName = user.UserName ?? user.Email,
                PasswordHash = passwordHash,
                user.CreatedAt,
                user.UpdatedAt
            }, transaction, cancellationToken: cancellationToken));

            foreach (var role in roles)
            {
                var roleId = await connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(getRoleSql, new { Name = role }, transaction, cancellationToken: cancellationToken));
                if (roleId is not null)
                {
                    await connection.ExecuteAsync(new CommandDefinition(insertRoleSql, new { UserId = user.Id, RoleId = roleId.Value }, transaction, cancellationToken: cancellationToken));
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT r.Name
                           FROM Roles r
                           INNER JOIN UserRoles ur ON ur.RoleId = r.Id
                           WHERE ur.UserId = @UserId
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var roles = await connection.QueryAsync<string>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return roles.ToArray();
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Users WHERE Id = @UserId AND IsDeleted = 0";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return count > 0;
    }
}
