using Dapper;
using WorkPulse.Application.DTOs.Users;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Constants;
using WorkPulse.Domain.Entities;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyCollection<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt
                           FROM Users
                           WHERE IsDeleted = 0
                           ORDER BY LastName, FirstName
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var users = await connection.QueryAsync<ApplicationUser>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return users.ToArray();
    }

    public async Task<IReadOnlyCollection<DeveloperDto>> GetDevelopersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT u.Id,
                                  u.FirstName,
                                  u.LastName,
                                  CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
                                  u.Email,
                                  SUM(CASE WHEN t.Status <> @Completed THEN 1 ELSE 0 END) AS ActiveTaskCount,
                                  SUM(CASE WHEN t.Status = @InProgress THEN 1 ELSE 0 END) AS InProgressTaskCount,
                                  SUM(CASE WHEN t.Status = @Completed THEN 1 ELSE 0 END) AS CompletedTaskCount
                           FROM Users u
                           INNER JOIN UserRoles ur ON ur.UserId = u.Id
                           INNER JOIN Roles r ON r.Id = ur.RoleId AND r.Name IN @AssignableRoles
                           LEFT JOIN Tasks t ON t.AssignedUserId = u.Id
                           WHERE u.IsDeleted = 0
                           GROUP BY u.Id, u.FirstName, u.LastName, u.Email
                           ORDER BY u.LastName, u.FirstName
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var developers = await connection.QueryAsync<DeveloperDto>(new CommandDefinition(sql, new
        {
            AssignableRoles = new[] { "Developer", "Admin" },
            Completed = (int)WorkPulse.Domain.Enums.TaskStatus.Completed,
            InProgress = (int)WorkPulse.Domain.Enums.TaskStatus.InProgress
        }, cancellationToken: cancellationToken));
        return developers.ToArray();
    }

    public async Task<IReadOnlyCollection<UserManagementDto>> GetUserManagementAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT u.Id,
                                  u.FirstName,
                                  u.LastName,
                                  CONCAT(u.FirstName, ' ', u.LastName) AS FullName,
                                  u.Email,
                                  u.CreatedAt,
                                  COALESCE(r.Name, @Pending) AS Role
                           FROM Users u
                           OUTER APPLY (
                               SELECT TOP 1 r.Name
                               FROM UserRoles ur
                               INNER JOIN Roles r ON r.Id = ur.RoleId
                               WHERE ur.UserId = u.Id
                               ORDER BY CASE r.Name WHEN @Admin THEN 1 WHEN @Developer THEN 2 WHEN @Pending THEN 3 ELSE 4 END
                           ) r
                           WHERE u.IsDeleted = 0
                           ORDER BY u.LastName, u.FirstName
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var users = await connection.QueryAsync<UserManagementDto>(new CommandDefinition(sql, new
        {
            Admin = Roles.Admin,
            Developer = Roles.Developer,
            Pending = Roles.Pending
        }, cancellationToken: cancellationToken));
        return users.ToArray();
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
        await connection.OpenAsync(cancellationToken);
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
                UserName = user.Email,
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

    public async Task<int> CountAdminsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM Users u
                           INNER JOIN UserRoles ur ON ur.UserId = u.Id
                           INNER JOIN Roles r ON r.Id = ur.RoleId
                           WHERE u.IsDeleted = 0
                             AND r.Name = @Admin
                           """;

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Admin = Roles.Admin }, cancellationToken: cancellationToken));
    }

    public async Task UpdateRoleAsync(string userId, string? role, CancellationToken cancellationToken = default)
    {
        const string deleteSql = "DELETE ur FROM UserRoles ur INNER JOIN Roles r ON r.Id = ur.RoleId WHERE ur.UserId = @UserId";
        const string insertSql = "INSERT INTO UserRoles (UserId, RoleId) SELECT @UserId, r.Id FROM Roles r WHERE r.Name = @RoleName";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition(deleteSql, new { UserId = userId }, transaction, cancellationToken: cancellationToken));

            if (!string.IsNullOrWhiteSpace(role) && !string.Equals(role, Roles.Pending, StringComparison.OrdinalIgnoreCase))
            {
                await connection.ExecuteAsync(new CommandDefinition(insertSql, new { UserId = userId, RoleName = role.Trim() }, transaction, cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Users WHERE Id = @UserId AND IsDeleted = 0";

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return count > 0;
    }
}
