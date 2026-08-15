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
        var seedUsers = await EnsureUsersAsync(connection, logger, adminPassword, cancellationToken);

        if (!_seedOptions.SeedSampleData)
        {
            logger.LogInformation("Development sample-data seeding is disabled.");
            logger.LogInformation("Seed completed successfully.");
            return;
        }

        if (!await HasSampleDataTablesAsync(connection, cancellationToken))
        {
            logger.LogWarning("Skipping development sample-data seed because Clients/Projects/Tasks/Sprints tables are missing. Identity users were still seeded.");
            logger.LogInformation("Seed completed successfully.");
            return;
        }

        await EnsureSampleDataAsync(connection, seedUsers, cancellationToken);

        logger.LogInformation("Seed completed successfully.");
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

    private async Task<SeedUsers> EnsureUsersAsync(Microsoft.Data.SqlClient.SqlConnection connection, ILogger logger, string adminPassword, CancellationToken cancellationToken)
    {
        var adminId = await EnsureUserAsync(connection, logger, new SeedUser("11111111-1111-1111-1111-111111111111", "Admin", "User", _seedOptions.AdminEmail, adminPassword, Roles.Admin), cancellationToken);
        var developerId = await EnsureUserAsync(connection, logger, new SeedUser("22222222-2222-2222-2222-222222222222", "Simphiwe", "Dlamini", "developer@workpulse.local", "WorkPulseDev123!", Roles.Developer), cancellationToken);
        var secondDeveloperId = await EnsureUserAsync(connection, logger, new SeedUser("33333333-3333-3333-3333-333333333333", "Lerato", "Molema", "developer2@workpulse.local", "WorkPulseDev234!", Roles.Developer), cancellationToken);
        var thirdDeveloperId = await EnsureUserAsync(connection, logger, new SeedUser("44444444-4444-4444-4444-444444444444", "Thabo", "Ndlovu", "developer3@workpulse.local", "WorkPulseDev345!", Roles.Developer), cancellationToken);
        var fourthDeveloperId = await EnsureUserAsync(connection, logger, new SeedUser("55555555-5555-5555-5555-555555555555", "Kagiso", "More", "developer4@workpulse.local", "WorkPulseDev456!", Roles.Developer), cancellationToken);

        return new SeedUsers(adminId, developerId, secondDeveloperId, thirdDeveloperId, fourthDeveloperId);
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

    private static async Task EnsureSampleDataAsync(Microsoft.Data.SqlClient.SqlConnection connection, SeedUsers seedUsers, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var today = utcNow.Date;

        var clients = new[]
        {
            new SeedClient("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "Northstar Financial", "Mia Jacobs", "mia.jacobs@northstar.example", "+27 11 555 0101", "Financial services and lending operations."),
            new SeedClient("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "Apex Retail Group", "Lerato Molefe", "lerato.molefe@apex.example", "+27 11 555 0102", "Retail commerce and logistics."),
            new SeedClient("cccccccc-cccc-cccc-cccc-cccccccccccc", "Vertex Logistics", "Thandi Nkosi", "thandi.nkosi@vertex.example", "+27 11 555 0103", "Fleet management and route planning."),
            new SeedClient("dddddddd-dddd-dddd-dddd-dddddddddddd", "Horizon Health", "Tebogo Mokoena", "tebogo.mokoena@horizon.example", "+27 11 555 0104", "Healthcare portals and patient workflows."),
            new SeedClient("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee", "CloudCore Solutions", "Nandi Dube", "nandi.dube@cloudcore.example", "+27 11 555 0105", "Developer tooling and platform engineering.")
        };

        foreach (var client in clients)
        {
            await EnsureClientAsync(connection, client, utcNow, cancellationToken);
        }

        var projects = new[]
        {
            new SeedProject("10000000-0000-0000-0000-000000000001", clients[0].Id, "Customer Portal", "Northstar customer self-service portal.", ProjectStatus.Active, today.AddDays(-45), today.AddDays(60)),
            new SeedProject("10000000-0000-0000-0000-000000000002", clients[0].Id, "Platform Revamp", "Core platform modernization and refactoring.", ProjectStatus.Active, today.AddDays(-30), today.AddDays(45)),
            new SeedProject("10000000-0000-0000-0000-000000000003", clients[0].Id, "Reporting Service", "Scheduled reporting and export service.", ProjectStatus.Active, today.AddDays(-20), today.AddDays(30)),
            new SeedProject("10000000-0000-0000-0000-000000000004", clients[1].Id, "Claims Platform", "Claims workflow and case management.", ProjectStatus.Active, today.AddDays(-35), today.AddDays(70)),
            new SeedProject("10000000-0000-0000-0000-000000000005", clients[1].Id, "Mobile App", "Retail mobile experience.", ProjectStatus.Active, today.AddDays(-25), today.AddDays(90)),
            new SeedProject("10000000-0000-0000-0000-000000000006", clients[1].Id, "Web App", "Commerce web storefront.", ProjectStatus.Active, today.AddDays(-15), today.AddDays(75)),
            new SeedProject("10000000-0000-0000-0000-000000000007", clients[2].Id, "Fleet System", "Dispatch and fleet operations.", ProjectStatus.Active, today.AddDays(-40), today.AddDays(50)),
            new SeedProject("10000000-0000-0000-0000-000000000008", clients[3].Id, "Patient Portal", "Patient self-service and support portal.", ProjectStatus.Active, today.AddDays(-28), today.AddDays(80)),
            new SeedProject("10000000-0000-0000-0000-000000000009", clients[4].Id, "Developer Platform", "Internal developer platform and tooling.", ProjectStatus.Active, today.AddDays(-18), today.AddDays(120))
        };

        foreach (var project in projects)
        {
            await EnsureProjectAsync(connection, project, utcNow, cancellationToken);
        }

        var projectLookup = projects.ToDictionary(project => project.Name, project => project.Id);

        var sprints = new[]
        {
            new SeedSprint("30000000-0000-0000-0000-000000000001", "Foundation Sprint", today.AddDays(-14), today.AddDays(0), SprintStatus.Completed),
            new SeedSprint("30000000-0000-0000-0000-000000000002", "Stabilization Sprint", today.AddDays(-7), today.AddDays(7), SprintStatus.Active),
            new SeedSprint("30000000-0000-0000-0000-000000000003", "Launch Sprint", today.AddDays(8), today.AddDays(22), SprintStatus.Planned)
        };

        foreach (var sprint in sprints)
        {
            await EnsureSprintAsync(connection, sprint, utcNow, cancellationToken);
        }

        var sprintLookup = sprints.ToDictionary(sprint => sprint.Name, sprint => sprint.Id);

        var tasks = new[]
        {
            new SeedTask("20000000-0000-0000-0000-000000000001", projectLookup["Customer Portal"], seedUsers.DeveloperUserId, sprintLookup["Stabilization Sprint"], "Payment Gateway Authentication Failure", "Authentication requests fail in the payment gateway token refresh flow.", today, TaskStatus.InProgress, TaskPriority.Critical, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000002", projectLookup["Claims Platform"], seedUsers.SecondDeveloperUserId, sprintLookup["Stabilization Sprint"], "API Response Timeout Investigation", "Investigate slow downstream responses in the claims API.", today.AddDays(-1), TaskStatus.Todo, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000003", projectLookup["Reporting Service"], seedUsers.DeveloperUserId, sprintLookup["Foundation Sprint"], "Fix Data Export Bug", "CSV exports are missing rows on the reporting endpoint.", today.AddDays(-2), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000004", projectLookup["Customer Portal"], seedUsers.DeveloperUserId, sprintLookup["Stabilization Sprint"], "Update Payment Integration", "Complete the payment integration update for the customer portal.", today, TaskStatus.Todo, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000005", projectLookup["Patient Portal"], seedUsers.ThirdDeveloperUserId, sprintLookup["Stabilization Sprint"], "User Login Error Handling", "Improve validation and error handling on the patient login form.", today, TaskStatus.Todo, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000006", projectLookup["Fleet System"], seedUsers.DeveloperUserId, sprintLookup["Launch Sprint"], "Add Audit Logging", "Add audit logging to key operational workflows.", today.AddDays(1), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000007", projectLookup["Mobile App"], seedUsers.FourthDeveloperUserId, sprintLookup["Launch Sprint"], "Refactor Notification Module", "Refactor the mobile notification module for maintainability.", today.AddDays(2), TaskStatus.Todo, TaskPriority.Low, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000008", projectLookup["Platform Revamp"], seedUsers.DeveloperUserId, sprintLookup["Foundation Sprint"], "Fix Button Alignment Issue", "UI alignment issue on the primary action buttons.", today.AddDays(-1), TaskStatus.Completed, TaskPriority.Low, utcNow.AddHours(-2), utcNow.AddHours(-2), utcNow.AddHours(-2)),
            new SeedTask("20000000-0000-0000-0000-000000000009", projectLookup["Patient Portal"], seedUsers.SecondDeveloperUserId, sprintLookup["Foundation Sprint"], "Update Privacy Policy", "Refresh the privacy policy acceptance content.", today.AddDays(-1), TaskStatus.Completed, TaskPriority.Low, utcNow.AddHours(-5), utcNow.AddHours(-5), utcNow.AddHours(-5)),
            new SeedTask("20000000-0000-0000-0000-000000000010", projectLookup["Web App"], seedUsers.FourthDeveloperUserId, sprintLookup["Foundation Sprint"], "Resolve Build Warning", "Fix a clean build warning in the web app pipeline.", today.AddDays(-2), TaskStatus.Completed, TaskPriority.Medium, utcNow.AddDays(-1).AddHours(-1), utcNow.AddDays(-1).AddHours(-1), utcNow.AddDays(-1).AddHours(-1)),
            new SeedTask("20000000-0000-0000-0000-000000000011", projectLookup["Fleet System"], seedUsers.ThirdDeveloperUserId, sprintLookup["Foundation Sprint"], "Improve Table Performance", "Optimize the large operational table rendering path.", today.AddDays(-2), TaskStatus.Completed, TaskPriority.Low, utcNow.AddDays(-1).AddHours(-2), utcNow.AddDays(-1).AddHours(-2), utcNow.AddDays(-1).AddHours(-2)),
            new SeedTask("20000000-0000-0000-0000-000000000012", projectLookup["Customer Portal"], seedUsers.DeveloperUserId, sprintLookup["Stabilization Sprint"], "Customer Portal API Token Refresh", "Finish the token refresh flow for portal sessions.", today.AddDays(1), TaskStatus.InProgress, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000013", projectLookup["Reporting Service"], seedUsers.SecondDeveloperUserId, sprintLookup["Launch Sprint"], "Invoice Export Review", "Review the invoice export data set for accuracy.", today.AddDays(7), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000014", projectLookup["Claims Platform"], seedUsers.ThirdDeveloperUserId, sprintLookup["Launch Sprint"], "Claims Workflow Mapping", "Document the claims workflow for the next delivery cycle.", today.AddDays(3), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000015", projectLookup["Mobile App"], seedUsers.FourthDeveloperUserId, sprintLookup["Stabilization Sprint"], "Mobile App Push Notification Polish", "Tighten the push notification experience.", today, TaskStatus.InProgress, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000016", projectLookup["Web App"], seedUsers.SecondDeveloperUserId, sprintLookup["Launch Sprint"], "Web App Accessibility Review", "Improve the accessibility score on the commerce web app.", today.AddDays(4), TaskStatus.Todo, TaskPriority.Low, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000017", projectLookup["Fleet System"], seedUsers.ThirdDeveloperUserId, sprintLookup["Launch Sprint"], "Route Optimization Draft", "Draft route optimization ideas for the fleet system.", today.AddDays(5), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000018", projectLookup["Fleet System"], seedUsers.FourthDeveloperUserId, sprintLookup["Launch Sprint"], "Driver Assignment Panel", "Prepare the driver assignment panel for internal use.", today, TaskStatus.Todo, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000019", projectLookup["Patient Portal"], seedUsers.SecondDeveloperUserId, sprintLookup["Stabilization Sprint"], "Appointment Reminder UI", "Add reminder improvements to the patient portal.", today.AddDays(1), TaskStatus.InProgress, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000020", projectLookup["Patient Portal"], seedUsers.DeveloperUserId, sprintLookup["Foundation Sprint"], "Portal Onboarding Copy", "Update the onboarding copy to match the approved wording.", today.AddDays(-3), TaskStatus.Completed, TaskPriority.Low, utcNow.AddDays(-3).AddHours(-2), utcNow.AddDays(-3).AddHours(-2), utcNow.AddDays(-3).AddHours(-2)),
            new SeedTask("20000000-0000-0000-0000-000000000021", projectLookup["Developer Platform"], seedUsers.ThirdDeveloperUserId, sprintLookup["Launch Sprint"], "Developer Platform Release Checklist", "Prepare the release checklist for the internal platform.", today, TaskStatus.Todo, TaskPriority.High, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000022", projectLookup["Developer Platform"], seedUsers.FourthDeveloperUserId, sprintLookup["Launch Sprint"], "API Key Rotation Task", "Rotate platform keys and validate access controls.", today, TaskStatus.InProgress, TaskPriority.Critical, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000023", projectLookup["Developer Platform"], seedUsers.DeveloperUserId, sprintLookup["Launch Sprint"], "Cache Warmup Improvements", "Improve startup cache warmup for the developer portal.", today.AddDays(2), TaskStatus.Todo, TaskPriority.Medium, utcNow, null, utcNow),
            new SeedTask("20000000-0000-0000-0000-000000000024", projectLookup["Developer Platform"], seedUsers.SecondDeveloperUserId, sprintLookup["Launch Sprint"], "Search Autocomplete Enhancement", "Polish search autocomplete for the internal platform.", today.AddDays(6), TaskStatus.Todo, TaskPriority.Low, utcNow, null, utcNow)
        };

        foreach (var task in tasks)
        {
            await EnsureTaskAsync(connection, task, cancellationToken);
        }
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
                                     INSERT INTO Tasks (Id, ProjectId, AssignedUserId, SprintId, Title, Description, DueDate, Status, Priority, CreatedAt, UpdatedAt, CompletedAt)
                                     VALUES (@Id, @ProjectId, @AssignedUserId, @SprintId, @Title, @Description, @DueDate, @Status, @Priority, @CreatedAt, @UpdatedAt, @CompletedAt)
                                     """;

            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                task.Id,
                task.ProjectId,
                task.AssignedUserId,
                task.SprintId,
                task.Title,
                task.Description,
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
                                     AssignedUserId = @AssignedUserId,
                                     SprintId = @SprintId,
                                     Title = @Title,
                                     Description = @Description,
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
            task.AssignedUserId,
            task.SprintId,
            task.Title,
            task.Description,
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
    private sealed record SeedTask(string Id, string ProjectId, string AssignedUserId, string SprintId, string Title, string Description, DateTime DueDate, TaskStatus Status, TaskPriority Priority, DateTime CreatedAt, DateTime? CompletedAt, DateTime UpdatedAt);
    private sealed record SeedUsers(string AdminUserId, string DeveloperUserId, string SecondDeveloperUserId, string ThirdDeveloperUserId, string FourthDeveloperUserId);
}
