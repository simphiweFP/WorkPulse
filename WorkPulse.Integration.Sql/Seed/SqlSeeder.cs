using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Constants;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Seed;

public sealed class SqlSeeder
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly DevelopmentSeedOptions _seedOptions;

    public SqlSeeder(IDbConnectionFactory connectionFactory, IPasswordHasher passwordHasher, IOptions<DevelopmentSeedOptions> seedOptions)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
        _seedOptions = seedOptions.Value;
    }

    public async Task SeedAsync(ILogger logger, CancellationToken cancellationToken = default)
    {
        if (!_seedOptions.Enabled)
        {
            return;
        }

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await EnsureRolesAsync(connection, cancellationToken);
        await EnsureAdminAsync(connection, logger, cancellationToken);

        if (_seedOptions.SeedSampleData)
        {
            await EnsureSampleDataAsync(connection, cancellationToken);
        }
    }

    private static async Task EnsureRolesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           MERGE Roles AS target
                           USING (VALUES (@Admin), (@Manager), (@Employee)) AS source (Name)
                           ON target.Name = source.Name
                           WHEN NOT MATCHED THEN INSERT (Name) VALUES (source.Name);
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Admin = Roles.Admin,
            Manager = Roles.Manager,
            Employee = Roles.Employee
        }, cancellationToken: cancellationToken));
    }

    private async Task EnsureAdminAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, CancellationToken cancellationToken)
    {
        var adminEmail = _seedOptions.AdminEmail.Trim().ToLowerInvariant();
        var adminPassword = _seedOptions.AdminPassword;

        const string existsSql = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(existsSql, new { Email = adminEmail }, cancellationToken: cancellationToken));
        if (exists > 0)
        {
            return;
        }

        var userId = Guid.NewGuid().ToString();
        const string insertUserSql = """
                                     INSERT INTO Users (Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt, IsDeleted)
                                     VALUES (@Id, @FirstName, @LastName, @Email, @UserName, @PasswordHash, @CreatedAt, @UpdatedAt, 0)
                                     """;

        await connection.ExecuteAsync(new CommandDefinition(insertUserSql, new
        {
            Id = userId,
            FirstName = "System",
            LastName = "Admin",
            Email = adminEmail,
            UserName = adminEmail,
            PasswordHash = _passwordHasher.Hash(adminPassword),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken));

        const string insertRoleSql = """
                                     INSERT INTO UserRoles (UserId, RoleId)
                                     SELECT @UserId, Id FROM Roles WHERE Name = @RoleName
                                     """;

        await connection.ExecuteAsync(new CommandDefinition(insertRoleSql, new { UserId = userId, RoleName = Roles.Admin }, cancellationToken: cancellationToken));
        logger.LogInformation("Seeded default development admin user: {Email}", adminEmail);
    }

    private static async Task EnsureSampleDataAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        var hasClients = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM Clients", cancellationToken: cancellationToken));
        if (hasClients > 0)
        {
            return;
        }

        var clientId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Clients (Id, Name, Email, Phone, Address, CreatedAt, UpdatedAt) VALUES (@Id, @Name, @Email, @Phone, @Address, @CreatedAt, @UpdatedAt)",
            new
            {
                Id = clientId,
                Name = "Northwind Systems",
                Email = "contact@northwind.example",
                Phone = "+1-555-0101",
                Address = "1420 Lakeview Ave",
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Projects (Id, ClientId, Name, Description, StartDate, EndDate, Status, CreatedAt, UpdatedAt) VALUES (@Id, @ClientId, @Name, @Description, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)",
            new
            {
                Id = projectId,
                ClientId = clientId,
                Name = "Inventory API Revamp",
                Description = "Modernize internal inventory APIs.",
                StartDate = now.Date,
                EndDate = (DateTime?)null,
                Status = (int)ProjectStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Tasks (Id, ProjectId, AssignedUserId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt) VALUES (@Id, @ProjectId, NULL, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, NULL)",
            new
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Create baseline API task model",
                Description = "Validate task CRUD from Swagger.",
                DueDate = now.Date.AddDays(1),
                Status = (int)TaskStatus.Todo,
                Priority = (int)TaskPriority.High,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken: cancellationToken));
    }
}
