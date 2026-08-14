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

        if (!await HasRequiredTablesAsync(connection, cancellationToken))
        {
            logger.LogWarning("Skipping development seed because the database schema is incomplete. Recreate the local database to enable seeding.");
            return;
        }

        await EnsureRolesAsync(connection, cancellationToken);
        var seedUsers = await EnsureUsersAsync(connection, logger, cancellationToken);

        if (_seedOptions.SeedSampleData)
        {
            await EnsureSampleDataAsync(connection, seedUsers, cancellationToken);
        }
    }

    private static async Task EnsureRolesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           MERGE Roles AS target
                           USING (VALUES (@Admin), (@Developer)) AS source (Name)
                           ON target.Name = source.Name
                           WHEN NOT MATCHED THEN INSERT (Name) VALUES (source.Name);
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Admin = Roles.Admin,
            Developer = Roles.Developer
        }, cancellationToken: cancellationToken));
    }

    private async Task<SeedUsers> EnsureUsersAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, CancellationToken cancellationToken)
    {
        var adminId = await EnsureUserAsync(connection, logger, new SeedUser("11111111-1111-1111-1111-111111111111", "System", "Admin", _seedOptions.AdminEmail, _seedOptions.AdminPassword, Roles.Admin), cancellationToken);
        var developerId = await EnsureUserAsync(connection, logger, new SeedUser("22222222-2222-2222-2222-222222222222", "Default", "Developer", "developer@workpulse.local", "WorkPulseDev123!", Roles.Developer), cancellationToken);
        var secondDeveloperId = await EnsureUserAsync(connection, logger, new SeedUser("33333333-3333-3333-3333-333333333333", "Second", "Developer", "developer2@workpulse.local", "WorkPulseDev234!", Roles.Developer), cancellationToken);

        return new SeedUsers(adminId, developerId, secondDeveloperId);
    }

    private async Task<string> EnsureUserAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, SeedUser user, CancellationToken cancellationToken)
    {
        const string existsSql = "SELECT Id FROM Users WHERE Email = @Email";
        var existingId = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(existsSql, new { Email = user.Email }, cancellationToken: cancellationToken));
        if (!string.IsNullOrWhiteSpace(existingId))
        {
            return existingId;
        }

        const string insertUserSql = """
                                     INSERT INTO Users (Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt, IsDeleted)
                                     VALUES (@Id, @FirstName, @LastName, @Email, @UserName, @PasswordHash, @CreatedAt, @UpdatedAt, 0)
                                     """;

        await connection.ExecuteAsync(new CommandDefinition(insertUserSql, new
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            UserName = user.Email,
            PasswordHash = _passwordHasher.Hash(user.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: cancellationToken));

        const string insertRoleSql = """
                                     INSERT INTO UserRoles (UserId, RoleId)
                                     SELECT @UserId, Id FROM Roles WHERE Name = @RoleName
                                     """;

        await connection.ExecuteAsync(new CommandDefinition(insertRoleSql, new { UserId = user.Id, RoleName = user.RoleName }, cancellationToken: cancellationToken));
        logger.LogInformation("Seeded development user: {Email}", user.Email);
        return user.Id;
    }

    private static async Task EnsureSampleDataAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedUsers seedUsers, CancellationToken cancellationToken)
    {
        var hasClients = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM Clients", cancellationToken: cancellationToken));
        if (hasClients > 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var clients = new[]
        {
            new { Id = Guid.NewGuid(), Name = "Northwind Systems", ContactName = "Northwind Office", ContactEmail = "contact@northwind.example", PhoneNumber = "+1-555-0101", Description = "Business systems and reporting" },
            new { Id = Guid.NewGuid(), Name = "Fabrikam Studio", ContactName = "Fabrikam Desk", ContactEmail = "hello@fabrikam.example", PhoneNumber = "+1-555-0102", Description = "Product design and delivery" }
        };

        foreach (var client in clients)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Clients (Id, Name, ContactName, ContactEmail, PhoneNumber, Description, CreatedAt, UpdatedAt, IsDeleted) VALUES (@Id, @Name, @ContactName, @ContactEmail, @PhoneNumber, @Description, @CreatedAt, @UpdatedAt, 0)",
                new
                {
                    client.Id,
                    client.Name,
                    client.ContactName,
                    client.ContactEmail,
                    client.PhoneNumber,
                    client.Description,
                    CreatedAt = now,
                    UpdatedAt = now
                }, cancellationToken: cancellationToken));
        }

        var projects = new[]
        {
            new { Id = Guid.NewGuid(), ClientId = clients[0].Id, Name = "Inventory API Revamp", Description = "Modernize internal inventory APIs.", Status = (int)ProjectStatus.Active },
            new { Id = Guid.NewGuid(), ClientId = clients[0].Id, Name = "Ops Dashboard", Description = "Track deployment and support work.", Status = (int)ProjectStatus.Active },
            new { Id = Guid.NewGuid(), ClientId = clients[1].Id, Name = "Client Portal Discovery", Description = "Prepare scoped portal requirements.", Status = (int)ProjectStatus.Active }
        };

        foreach (var project in projects)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Projects (Id, ClientId, Name, Description, Status, CreatedAt, UpdatedAt) VALUES (@Id, @ClientId, @Name, @Description, @Status, @CreatedAt, @UpdatedAt)",
                new
                {
                    project.Id,
                    project.ClientId,
                    project.Name,
                    project.Description,
                    project.Status,
                    CreatedAt = now,
                    UpdatedAt = now
                }, cancellationToken: cancellationToken));
        }

        var tasks = new[]
        {
            new { ProjectId = projects[0].Id, AssignedUserId = seedUsers.DeveloperUserId, Title = "Resolve overdue API bug", Description = "Fix the failing endpoint before the daily check-in.", DueDate = now.Date.AddDays(-1), Status = (int)TaskStatus.InProgress, Priority = (int)TaskPriority.Critical },
            new { ProjectId = projects[0].Id, AssignedUserId = seedUsers.DeveloperUserId, Title = "Prepare today deployment notes", Description = "Document the release steps for the admin review.", DueDate = now.Date, Status = (int)TaskStatus.Todo, Priority = (int)TaskPriority.High },
            new { ProjectId = projects[1].Id, AssignedUserId = seedUsers.SecondDeveloperUserId, Title = "Validate critical upcoming release", Description = "Confirm smoke test coverage before the deadline.", DueDate = now.Date.AddDays(2), Status = (int)TaskStatus.Todo, Priority = (int)TaskPriority.Critical },
            new { ProjectId = projects[1].Id, AssignedUserId = seedUsers.DeveloperUserId, Title = "Plan high priority backlog item", Description = "Review design and estimate the work.", DueDate = now.Date.AddDays(3), Status = (int)TaskStatus.Todo, Priority = (int)TaskPriority.High },
            new { ProjectId = projects[2].Id, AssignedUserId = seedUsers.SecondDeveloperUserId, Title = "Long horizon low priority task", Description = "This should stay out of Today recommendations.", DueDate = now.Date.AddDays(10), Status = (int)TaskStatus.Todo, Priority = (int)TaskPriority.Low },
            new { ProjectId = projects[2].Id, AssignedUserId = seedUsers.SecondDeveloperUserId, Title = "Completed delivery wrap-up", Description = "Already finished and should not appear in Today.", DueDate = now.Date.AddDays(1), Status = (int)TaskStatus.Completed, Priority = (int)TaskPriority.Medium }
        };

        foreach (var task in tasks)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Tasks (Id, ProjectId, AssignedUserId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt) VALUES (@Id, @ProjectId, @AssignedUserId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)",
                new
                {
                    Id = Guid.NewGuid(),
                    task.ProjectId,
                    task.AssignedUserId,
                    task.Title,
                    task.Description,
                    task.DueDate,
                    task.Status,
                    task.Priority,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CompletedAt = task.Status == (int)TaskStatus.Completed ? (DateTime?)now : null
                }, cancellationToken: cancellationToken));
        }
    }

    private static async Task<bool> HasRequiredTablesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.TABLES
                           WHERE TABLE_SCHEMA = 'dbo'
                             AND TABLE_NAME IN ('Users', 'Roles', 'UserRoles', 'Clients', 'Projects', 'Tasks')
                           """;

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return count >= 6;
    }

    private sealed record SeedUser(string Id, string FirstName, string LastName, string Email, string Password, string RoleName);

    private sealed record SeedUsers(string AdminUserId, string DeveloperUserId, string SecondDeveloperUserId);
}
