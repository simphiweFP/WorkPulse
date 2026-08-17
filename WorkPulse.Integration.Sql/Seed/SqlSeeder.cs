using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Constants;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Seed;

public sealed class SqlSeeder : IDatabaseSeeder
{
    private const string DefaultAdminPassword = "WorkPulseAdmin123!";
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

        var adminPassword = _seedOptions.AdminPassword;
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            adminPassword = DefaultAdminPassword;
            logger.LogWarning("DevelopmentSeed:AdminPassword was not configured. Falling back to default development admin password.");
        }

        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (!await HasIdentityTablesAsync(connection, cancellationToken))
        {
            logger.LogWarning("Skipping development seed because identity tables are missing. Run migrations before seeding.");
            return;
        }

        logger.LogInformation("Running development seed...");
        await EnsureRolesAsync(connection, cancellationToken);
        await EnsureUsersAsync(connection, logger, adminPassword, cancellationToken);

        logger.LogInformation("Seed completed successfully.");
    }

    private static async Task EnsureRolesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           MERGE Roles AS target
                           USING (VALUES (@Pending), (@Admin), (@Developer)) AS source (Name)
                           ON target.Name = source.Name
                           WHEN NOT MATCHED THEN INSERT (Name) VALUES (source.Name);
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Pending = Roles.Pending,
            Admin = Roles.Admin,
            Developer = Roles.Developer
        }, cancellationToken: cancellationToken));
    }

    private async Task<SeedUsers> EnsureUsersAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, string adminPassword, CancellationToken cancellationToken)
    {
        var adminId = await EnsureUserAsync(connection, logger, new SeedUser("11111111-1111-1111-1111-111111111111", "Admin", "User", _seedOptions.AdminEmail, adminPassword, Roles.Admin), cancellationToken);
        var developerId = await EnsureUserAsync(connection, logger, new SeedUser("22222222-2222-2222-2222-222222222222", "Simphiwe", "Dlamini", "developer@workpulse.local", "WorkPulseDev123!", Roles.Developer), cancellationToken);
        var pendingId = await EnsureUserAsync(connection, logger, new SeedUser("33333333-3333-3333-3333-333333333333", "Awaiting", "User", "pending@workpulse.local", "WorkPulsePending123!", Roles.Pending), cancellationToken);

        return new SeedUsers(adminId, developerId, pendingId);
    }

    private async Task<string> EnsureUserAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, SeedUser user, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT Id FROM Users WHERE Email = @Email";
        var existingId = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(selectSql, new { Email = user.Email }, cancellationToken: cancellationToken));

        var utcNow = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(existingId))
        {
            const string insertSql = """
                                     INSERT INTO Users (Id, FirstName, LastName, Email, UserName, PasswordHash, CreatedAt, UpdatedAt, IsDeleted)
                                     VALUES (@Id, @FirstName, @LastName, @Email, @UserName, @PasswordHash, @CreatedAt, @UpdatedAt, 0)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                Id = user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                UserName = user.Email,
                PasswordHash = _passwordHasher.Hash(user.Password),
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            }, cancellationToken: cancellationToken));

            logger.LogInformation("Seeded development user: {Email}", user.Email);
            existingId = user.Id;
        }
        else
        {
            const string updateSql = """
                                     UPDATE Users
                                     SET FirstName = @FirstName,
                                         LastName = @LastName,
                                         UserName = @UserName,
                                         UpdatedAt = @UpdatedAt,
                                         IsDeleted = 0
                                     WHERE Id = @Id
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(updateSql, new
            {
                Id = existingId,
                user.FirstName,
                user.LastName,
                UserName = user.Email,
                UpdatedAt = utcNow
            }, cancellationToken: cancellationToken));
        }

        await EnsureUserRoleAsync(connection, existingId, user.RoleName, cancellationToken);
        return existingId;
    }

    private static async Task EnsureUserRoleAsync(Microsoft.Data.SqlClient.SqlConnection connection, string userId, string roleName, CancellationToken cancellationToken)
    {
        if (string.Equals(roleName, Roles.Pending, StringComparison.OrdinalIgnoreCase))
        {
            const string deleteSql = "DELETE ur FROM UserRoles ur INNER JOIN Roles r ON r.Id = ur.RoleId WHERE ur.UserId = @UserId";
            await connection.ExecuteAsync(new CommandDefinition(deleteSql, new { UserId = userId }, cancellationToken: cancellationToken));
            return;
        }

        const string sql = """
                           INSERT INTO UserRoles (UserId, RoleId)
                           SELECT @UserId, r.Id
                           FROM Roles r
                           WHERE r.Name = @RoleName
                             AND NOT EXISTS (
                                 SELECT 1 FROM UserRoles ur WHERE ur.UserId = @UserId AND ur.RoleId = r.Id
                             )
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { UserId = userId, RoleName = roleName }, cancellationToken: cancellationToken));
    }

    private static Task EnsureSampleDataAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedUsers seedUsers, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task EnsureClientAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedClient client, DateTime utcNow, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT Id FROM Clients WHERE Id = @Id OR ContactEmail = @ContactEmail";
        var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(selectSql, new { client.Id, client.ContactEmail }, cancellationToken: cancellationToken));

        if (existingId is null)
        {
            const string insertSql = """
                                     INSERT INTO Clients (Id, Name, ContactName, ContactEmail, PhoneNumber, Description, CreatedAt, UpdatedAt, IsDeleted)
                                     VALUES (@Id, @Name, @ContactName, @ContactEmail, @PhoneNumber, @Description, @CreatedAt, @UpdatedAt, 0)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                client.Id,
                client.Name,
                client.ContactName,
                client.ContactEmail,
                client.PhoneNumber,
                client.Description,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            }, cancellationToken: cancellationToken));
            return;
        }

        const string updateSql = """
                                 UPDATE Clients
                                 SET Name = @Name,
                                     ContactName = @ContactName,
                                     ContactEmail = @ContactEmail,
                                     PhoneNumber = @PhoneNumber,
                                     Description = @Description,
                                     UpdatedAt = @UpdatedAt,
                                     IsDeleted = 0
                                 WHERE Id = @Id
                                 """;

        await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            Id = existingId.Value,
            client.Name,
            client.ContactName,
            client.ContactEmail,
            client.PhoneNumber,
            client.Description,
            UpdatedAt = utcNow
        }, cancellationToken: cancellationToken));
    }

    private static async Task EnsureProjectAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedProject project, DateTime utcNow, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT Id FROM Projects WHERE Id = @Id";
        var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(selectSql, new { project.Id }, cancellationToken: cancellationToken));

        if (existingId is null)
        {
            const string insertSql = """
                                     INSERT INTO Projects (Id, ClientId, Name, Description, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                                     VALUES (@Id, @ClientId, @Name, @Description, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                project.Id,
                project.ClientId,
                project.Name,
                project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = (int)project.Status,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            }, cancellationToken: cancellationToken));
            return;
        }

        const string updateSql = """
                                 UPDATE Projects
                                 SET ClientId = @ClientId,
                                     Name = @Name,
                                     Description = @Description,
                                     StartDate = @StartDate,
                                     EndDate = @EndDate,
                                     Status = @Status,
                                     UpdatedAt = @UpdatedAt
                                 WHERE Id = @Id
                                 """;

        await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            Id = existingId.Value,
            project.ClientId,
            project.Name,
            project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Status = (int)project.Status,
            UpdatedAt = utcNow
        }, cancellationToken: cancellationToken));
    }

    private static async Task EnsureSprintAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedSprint sprint, DateTime utcNow, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT Id FROM Sprints WHERE Id = @Id";
        var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(selectSql, new { sprint.Id }, cancellationToken: cancellationToken));

        if (existingId is null)
        {
            const string insertSql = """
                                     INSERT INTO Sprints (Id, Name, StartDate, EndDate, Status, CreatedAt, UpdatedAt)
                                     VALUES (@Id, @Name, @StartDate, @EndDate, @Status, @CreatedAt, @UpdatedAt)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                sprint.Id,
                sprint.Name,
                sprint.StartDate,
                sprint.EndDate,
                Status = (int)sprint.Status,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            }, cancellationToken: cancellationToken));
            return;
        }

        const string updateSql = """
                                 UPDATE Sprints
                                 SET Name = @Name,
                                     StartDate = @StartDate,
                                     EndDate = @EndDate,
                                     Status = @Status,
                                     UpdatedAt = @UpdatedAt
                                 WHERE Id = @Id
                                 """;

        await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            Id = existingId.Value,
            sprint.Name,
            sprint.StartDate,
            sprint.EndDate,
            Status = (int)sprint.Status,
            UpdatedAt = utcNow
        }, cancellationToken: cancellationToken));
    }

    private static async Task EnsureTaskAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedTask task, CancellationToken cancellationToken)
    {
        const string selectSql = "SELECT Id FROM Tasks WHERE Id = @Id";
        var existingId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(selectSql, new { task.Id }, cancellationToken: cancellationToken));

        if (existingId is null)
        {
            const string insertSql = """
                                     INSERT INTO Tasks (Id, ProjectId, TaskType, AssignedUserId, SprintId, Title, Description, StoryPoints, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt)
                                     VALUES (@Id, @ProjectId, @TaskType, @AssignedUserId, @SprintId, @Title, @Description, @StoryPoints, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                task.Id,
                task.ProjectId,
                TaskType = (int)task.Type,
                task.AssignedUserId,
                task.SprintId,
                task.Title,
                task.Description,
                StoryPoints = task.StoryPoints,
                DueDate = task.DueDate,
                Status = (int)task.Status,
                Priority = (int)task.Priority,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                task.CompletedAt
            }, cancellationToken: cancellationToken));
            return;
        }

            const string updateSql = """
                                     UPDATE Tasks
                                     SET ProjectId = @ProjectId,
                                         TaskType = @TaskType,
                                         AssignedUserId = @AssignedUserId,
                                         SprintId = @SprintId,
                                         Title = @Title,
                                         Description = @Description,
                                          StoryPoints = @StoryPoints,
                                         DueDate = @DueDate,
                                         Status = @Status,
                                         Priority = @Priority,
                                         UpdatedAt = @UpdatedAt,
                                         CompletedAt = @CompletedAt
                                     WHERE Id = @Id
                                     """;

        await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            Id = existingId.Value,
            task.ProjectId,
            TaskType = (int)task.Type,
            task.AssignedUserId,
            task.SprintId,
            task.Title,
            task.Description,
            StoryPoints = task.StoryPoints,
            DueDate = task.DueDate,
            Status = (int)task.Status,
            Priority = (int)task.Priority,
            UpdatedAt = task.UpdatedAt,
            task.CompletedAt
        }, cancellationToken: cancellationToken));
    }

    private static async Task<bool> HasIdentityTablesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.TABLES
                           WHERE TABLE_SCHEMA = 'dbo'
                             AND TABLE_NAME IN ('Users', 'Roles', 'UserRoles')
                           """;

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return count >= 3;
    }

    private static async Task<bool> HasSampleDataTablesAsync(Microsoft.Data.SqlClient.SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM INFORMATION_SCHEMA.TABLES
                           WHERE TABLE_SCHEMA = 'dbo'
                             AND TABLE_NAME IN ('Clients', 'Projects', 'Tasks', 'Sprints')
                           """;

        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return count >= 3;
    }

    private sealed record SeedUser(string Id, string FirstName, string LastName, string Email, string Password, string RoleName);
    private sealed record SeedClient(string Id, string Name, string ContactName, string ContactEmail, string PhoneNumber, string Description);
    private sealed record SeedProject(string Id, string ClientId, string Name, string Description, ProjectStatus Status, DateTime StartDate, DateTime? EndDate);
    private sealed record SeedSprint(string Id, string Name, DateTime StartDate, DateTime EndDate, SprintStatus Status);
    private sealed record SeedTask(string Id, string ProjectId, string? AssignedUserId, string? SprintId, string Title, string Description, DateTime DueDate, TaskStatus Status, TaskPriority Priority, DateTime CreatedAt, DateTime? CompletedAt, DateTime UpdatedAt)
    {
        public int StoryPoints { get; init; } = 0;
        public TaskType Type { get; init; } = TaskType.Story;
    }
    private sealed record SeedUsers(string AdminUserId, string DeveloperUserId, string PendingUserId);
}
