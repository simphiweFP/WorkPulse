using Dapper;
using FluentMigrator.Runner;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Integration.Sql.Migrations;

public sealed class MigrationRunner
{
    private readonly IMigrationRunner _migrationRunner;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(IMigrationRunner migrationRunner, IDbConnectionFactory connectionFactory, ILogger<MigrationRunner> logger)
    {
        _migrationRunner = migrationRunner;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task MigrateUpAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sqlConnection = connection as SqlConnection;

        if (sqlConnection is not null)
        {
            await sqlConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        var hasVersionInfo = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'VersionInfo'",
            cancellationToken: cancellationToken)) > 0;

        var existingCoreTables = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('Users', 'Clients', 'Projects', 'Tasks', 'Sprints')",
            cancellationToken: cancellationToken));

        var hasUsersTable = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users'",
            cancellationToken: cancellationToken)) > 0;

        var hasSprintsTable = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sprints'",
            cancellationToken: cancellationToken)) > 0;

        var hasSprintSupport = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'SprintId'",
            cancellationToken: cancellationToken)) > 0;

        var hasSprintOrder = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'SprintOrder'",
            cancellationToken: cancellationToken)) > 0;

        var hasTasksTable = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks'",
            cancellationToken: cancellationToken)) > 0;

        var hasSprintProject = hasSprintsTable && await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sprints' AND COLUMN_NAME = 'ProjectId'",
            cancellationToken: cancellationToken)) > 0;

        var hasSprintTotalTasks = hasSprintsTable && await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sprints' AND COLUMN_NAME = 'TotalTasks'",
            cancellationToken: cancellationToken)) > 0;

        var hasProjectsTable = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Projects'",
            cancellationToken: cancellationToken)) > 0;

        var hasTaskStoryPoints = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'StoryPoints'",
            cancellationToken: cancellationToken)) > 0;

        var hasTaskType = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'TaskType'",
            cancellationToken: cancellationToken)) > 0;

        var hasProjectTotalTasks = hasProjectsTable && await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Projects' AND COLUMN_NAME = 'TotalTasks'",
            cancellationToken: cancellationToken)) > 0;

        if (existingCoreTables > 0 && !hasUsersTable)
        {
            if (sqlConnection is null)
            {
                _logger.LogWarning("Skipping identity table repair because the SQL connection could not be opened as SqlConnection.");
                return;
            }

            _logger.LogInformation("Patching missing identity tables...");
            await EnsureIdentityTablesAsync(sqlConnection, cancellationToken);
            _logger.LogInformation("Identity table patch completed successfully.");
            hasUsersTable = true;
        }

        if (existingCoreTables > 0 && !hasTasksTable)
        {
            if (sqlConnection is null)
            {
                _logger.LogWarning("Skipping task table repair because the SQL connection could not be opened as SqlConnection.");
                return;
            }

            _logger.LogInformation("Patching missing task table...");
            await EnsureTaskTableAsync(sqlConnection, cancellationToken);
            _logger.LogInformation("Task table patch completed successfully.");
            hasTasksTable = true;
            hasSprintSupport = true;
            hasSprintOrder = true;
            hasTaskStoryPoints = true;
            hasTaskType = true;
        }

        if (hasVersionInfo && existingCoreTables is > 0 and < 4)
        {
            _logger.LogWarning("Skipping FluentMigrator because the database appears partially provisioned. Recreate the local database for a clean migration run.");
            return;
        }

        if (!hasVersionInfo)
        {
            if (existingCoreTables >= 4)
            {
                if (sqlConnection is null)
                {
                    _logger.LogWarning("Skipping sprint support patch because the SQL connection could not be opened as SqlConnection.");
                    return;
                }

                if (!hasSprintSupport || !hasSprintProject || !hasSprintOrder)
                {
                    _logger.LogInformation("Patching existing schema with sprint support...");
                    await EnsureSprintSupportAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Sprint support patch completed successfully.");
                }

                if (!hasSprintTotalTasks)
                {
                    _logger.LogInformation("Patching existing schema with sprint total tasks support...");
                    await EnsureSprintTotalTasksAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Sprint total tasks patch completed successfully.");
                }

                if (!hasProjectTotalTasks)
                {
                    _logger.LogInformation("Patching existing schema with project total tasks support...");
                    await EnsureProjectTotalTasksAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Project total tasks patch completed successfully.");
                }

                if (!hasTaskStoryPoints)
                {
                    _logger.LogInformation("Patching existing schema with task story points support...");
                    await EnsureTaskStoryPointsAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Task story points patch completed successfully.");
                }

                if (!hasTaskType)
                {
                    _logger.LogInformation("Patching existing schema with task type support...");
                    await EnsureTaskTypeAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Task type patch completed successfully.");
                }

                return;
            }

            if (existingCoreTables > 0)
            {
                _logger.LogWarning("Skipping FluentMigrator because a partial schema already exists without VersionInfo. Recreate the local database for a clean migration run.");
                return;
            }
        }

        if (hasTasksTable)
        {
            if (hasVersionInfo && (!hasSprintSupport || !hasSprintProject || !hasSprintOrder))
            {
                if (sqlConnection is null)
                {
                    _logger.LogWarning("Skipping sprint support patch because the SQL connection could not be opened as SqlConnection.");
                    return;
                }

                _logger.LogInformation("Patching existing migrated schema with sprint support...");
                await EnsureSprintSupportAsync(sqlConnection, cancellationToken);
                _logger.LogInformation("Sprint support patch completed successfully.");
            }

            if (!hasSprintTotalTasks)
            {
                if (sqlConnection is null)
                {
                    _logger.LogWarning("Skipping sprint total tasks patch because the SQL connection could not be opened as SqlConnection.");
                    return;
                }

                _logger.LogInformation("Patching existing migrated schema with sprint total tasks support...");
                await EnsureSprintTotalTasksAsync(sqlConnection, cancellationToken);
                _logger.LogInformation("Sprint total tasks patch completed successfully.");
            }
        }
        else
        {
            _logger.LogWarning("Skipping task-related repair because the Tasks table does not exist yet.");
        }

        if (!hasProjectTotalTasks)
        {
            if (!hasProjectsTable)
            {
                _logger.LogWarning("Skipping project total tasks patch because the Projects table does not exist yet.");
            }

            else if (sqlConnection is null)
            {
                _logger.LogWarning("Skipping project total tasks patch because the SQL connection could not be opened as SqlConnection.");
                return;
            }

            else
            {
                _logger.LogInformation("Patching existing migrated schema with project total tasks support...");
                await EnsureProjectTotalTasksAsync(sqlConnection, cancellationToken);
                _logger.LogInformation("Project total tasks patch completed successfully.");
            }
        }

        if (hasTasksTable)
        {
            if (!hasTaskStoryPoints)
            {
                if (sqlConnection is null)
                {
                    _logger.LogWarning("Skipping task story points patch because the SQL connection could not be opened as SqlConnection.");
                    return;
                }

                _logger.LogInformation("Patching existing migrated schema with task story points support...");
                await EnsureTaskStoryPointsAsync(sqlConnection, cancellationToken);
                _logger.LogInformation("Task story points patch completed successfully.");
            }

            if (!hasTaskType)
            {
                if (sqlConnection is null)
                {
                    _logger.LogWarning("Skipping task type patch because the SQL connection could not be opened as SqlConnection.");
                    return;
                }

                _logger.LogInformation("Patching existing migrated schema with task type support...");
                await EnsureTaskTypeAsync(sqlConnection, cancellationToken);
                _logger.LogInformation("Task type patch completed successfully.");
            }
        }

        _logger.LogInformation("Running FluentMigrator migrations...");
        _migrationRunner.MigrateUp();
        _logger.LogInformation("Migrations completed successfully.");
    }

    private static async Task EnsureSprintSupportAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string ensureSprintIdSql = """
                                           IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           BEGIN
                                               IF COL_LENGTH('dbo.Tasks', 'SprintId') IS NULL
                                               BEGIN
                                                   ALTER TABLE dbo.Tasks ADD SprintId UNIQUEIDENTIFIER NULL;
                                               END;
                                           END;
                                           """;

        const string ensureSprintOrderSql = """
                                             IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                             BEGIN
                                                 IF COL_LENGTH('dbo.Tasks', 'SprintOrder') IS NULL
                                                 BEGIN
                                                     ALTER TABLE dbo.Tasks ADD SprintOrder INT NULL;
                                                 END;
                                             END;
                                            """;

        const string ensureSprintTableSql = """
                                            IF OBJECT_ID('dbo.Sprints', 'U') IS NULL
                                            BEGIN
                                                CREATE TABLE dbo.Sprints (
                                                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                                                    ProjectId UNIQUEIDENTIFIER NOT NULL,
                                                    Name NVARCHAR(200) NOT NULL,
                                                    StartDate DATETIME2 NOT NULL,
                                                    EndDate DATETIME2 NOT NULL,
                                                    Status INT NOT NULL,
                                                    TotalTasks INT NOT NULL CONSTRAINT DF_Sprints_TotalTasks DEFAULT(0),
                                                    CreatedAt DATETIME2 NOT NULL,
                                                    UpdatedAt DATETIME2 NOT NULL
                                                );
                                            END;
                                            """;

        await connection.ExecuteAsync(new CommandDefinition(ensureSprintIdSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureSprintOrderSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureSprintTableSql, cancellationToken: cancellationToken));

        var hasSprintProject = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sprints' AND COLUMN_NAME = 'ProjectId'",
            cancellationToken: cancellationToken)) > 0;

        if (!hasSprintProject)
        {
            const string addProjectColumnSql = "ALTER TABLE dbo.Sprints ADD ProjectId UNIQUEIDENTIFIER NULL;";
            await connection.ExecuteAsync(new CommandDefinition(addProjectColumnSql, cancellationToken: cancellationToken));
        }

        const string promoteProjectColumnSql = """
                                               IF EXISTS (
                                                   SELECT 1
                                                   FROM dbo.Sprints
                                                   WHERE ProjectId IS NULL
                                               )
                                               BEGIN
                                                   RETURN;
                                               END;

                                               ALTER TABLE dbo.Sprints ALTER COLUMN ProjectId UNIQUEIDENTIFIER NOT NULL;
                                               """;

        await connection.ExecuteAsync(new CommandDefinition(promoteProjectColumnSql, cancellationToken: cancellationToken));

        const string ensureForeignKeySql = """
                                          IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Sprints_Projects_ProjectId')
                                          BEGIN
                                              ALTER TABLE dbo.Sprints
                                              ADD CONSTRAINT FK_Sprints_Projects_ProjectId
                                              FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id);
                                          END;
                                          """;

        const string ensureIndexSql = """
                                      IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Sprints_ProjectId' AND object_id = OBJECT_ID('dbo.Sprints'))
                                      BEGIN
                                          CREATE INDEX IX_Sprints_ProjectId ON dbo.Sprints(ProjectId);
                                      END;

                                      IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Sprints_SprintId')
                                      BEGIN
                                          ALTER TABLE dbo.Tasks
                                          ADD CONSTRAINT FK_Tasks_Sprints_SprintId
                                          FOREIGN KEY (SprintId) REFERENCES dbo.Sprints(Id);
                                      END;

                                      IF NOT EXISTS (
                                          SELECT 1
                                          FROM sys.indexes
                                          WHERE name = 'IX_Tasks_SprintId'
                                            AND object_id = OBJECT_ID('dbo.Tasks')
                                      )
                                      BEGIN
                                          CREATE INDEX IX_Tasks_SprintId ON dbo.Tasks(SprintId);
                                      END;

                                      IF NOT EXISTS (
                                          SELECT 1
                                          FROM sys.indexes
                                          WHERE name = 'IX_Tasks_SprintOrder'
                                            AND object_id = OBJECT_ID('dbo.Tasks')
                                      )
                                      BEGIN
                                          CREATE INDEX IX_Tasks_SprintOrder ON dbo.Tasks(SprintOrder);
                                      END;
                                      """;

        await connection.ExecuteAsync(new CommandDefinition(ensureForeignKeySql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureIndexSql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureProjectTotalTasksAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF COL_LENGTH('dbo.Projects', 'TotalTasks') IS NULL
                           BEGIN
                               ALTER TABLE dbo.Projects ADD TotalTasks INT NOT NULL CONSTRAINT DF_Projects_TotalTasks DEFAULT(0);
                           END;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureIdentityTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string usersSql = """
                                IF OBJECT_ID('dbo.Users', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE dbo.Users (
                                        Id NVARCHAR(64) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                                        FirstName NVARCHAR(100) NOT NULL,
                                        LastName NVARCHAR(100) NOT NULL,
                                        Email NVARCHAR(256) NOT NULL,
                                        UserName NVARCHAR(256) NOT NULL,
                                        PasswordHash NVARCHAR(MAX) NOT NULL,
                                        CreatedAt DATETIME2 NOT NULL,
                                        UpdatedAt DATETIME2 NOT NULL,
                                        IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT(0)
                                    );
                                END;

                                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
                                BEGIN
                                    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
                                END;
                                """;

        const string rolesSql = """
                                IF OBJECT_ID('dbo.Roles', 'U') IS NULL
                                BEGIN
                                    CREATE TABLE dbo.Roles (
                                        Id INT NOT NULL IDENTITY(1,1) CONSTRAINT PK_Roles PRIMARY KEY,
                                        Name NVARCHAR(50) NOT NULL
                                    );
                                END;

                                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_Name' AND object_id = OBJECT_ID('dbo.Roles'))
                                BEGIN
                                    CREATE UNIQUE INDEX IX_Roles_Name ON dbo.Roles(Name);
                                END;
                                """;

        const string userRolesSql = """
                                    IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
                                    BEGIN
                                        CREATE TABLE dbo.UserRoles (
                                            UserId NVARCHAR(64) NOT NULL,
                                            RoleId INT NOT NULL,
                                            CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId)
                                        );
                                    END;

                                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRoles_Users_UserId')
                                    BEGIN
                                        ALTER TABLE dbo.UserRoles
                                        ADD CONSTRAINT FK_UserRoles_Users_UserId
                                        FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
                                    END;

                                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserRoles_Roles_RoleId')
                                    BEGIN
                                        ALTER TABLE dbo.UserRoles
                                        ADD CONSTRAINT FK_UserRoles_Roles_RoleId
                                        FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
                                    END;
                                    """;

        await connection.ExecuteAsync(new CommandDefinition(usersSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(rolesSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(userRolesSql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureSprintTotalTasksAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID('dbo.Sprints', 'U') IS NOT NULL
                           BEGIN
                               IF COL_LENGTH('dbo.Sprints', 'TotalTasks') IS NULL
                               BEGIN
                                   ALTER TABLE dbo.Sprints ADD TotalTasks INT NOT NULL CONSTRAINT DF_Sprints_TotalTasks DEFAULT(0);
                               END;
                           END;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureTaskStoryPointsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                           BEGIN
                               IF COL_LENGTH('dbo.Tasks', 'StoryPoints') IS NULL
                               BEGIN
                                   ALTER TABLE dbo.Tasks ADD StoryPoints INT NOT NULL CONSTRAINT DF_Tasks_StoryPoints DEFAULT(0);
                               END;
                           END;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureTaskTypeAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                           BEGIN
                               IF COL_LENGTH('dbo.Tasks', 'TaskType') IS NULL
                               BEGIN
                                   ALTER TABLE dbo.Tasks ADD TaskType INT NOT NULL CONSTRAINT DF_Tasks_TaskType DEFAULT(2);
                               END;
                           END;
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task EnsureTaskTableAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string createTableSql = """
                                       IF OBJECT_ID('dbo.Tasks', 'U') IS NULL
                                       BEGIN
                                           CREATE TABLE dbo.Tasks (
                                               Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
                                               ProjectId UNIQUEIDENTIFIER NOT NULL,
                                               SprintId UNIQUEIDENTIFIER NULL,
                                               TaskType INT NOT NULL CONSTRAINT DF_Tasks_TaskType DEFAULT(2),
                                               AssignedUserId NVARCHAR(64) NULL,
                                               Title NVARCHAR(200) NOT NULL,
                                               Description NVARCHAR(2000) NOT NULL,
                                               StoryPoints INT NOT NULL CONSTRAINT DF_Tasks_StoryPoints DEFAULT(0),
                                               SprintOrder INT NULL,
                                               DueDate DATETIME2 NULL,
                                               Status INT NOT NULL,
                                               Priority INT NOT NULL,
                                               CreatedAt DATETIME2 NOT NULL,
                                               UpdatedAt DATETIME2 NOT NULL,
                                               CompletedAt DATETIME2 NULL
                                           );
                                       END;
                                       """;

        const string ensureProjectFkSql = """
                                          IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                             AND OBJECT_ID('dbo.Projects', 'U') IS NOT NULL
                                             AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Projects_ProjectId')
                                          BEGIN
                                              ALTER TABLE dbo.Tasks
                                              ADD CONSTRAINT FK_Tasks_Projects_ProjectId
                                              FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id);
                                          END;
                                          """;

        const string ensureSprintFkSql = """
                                         IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                            AND OBJECT_ID('dbo.Sprints', 'U') IS NOT NULL
                                            AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Sprints_SprintId')
                                         BEGIN
                                             ALTER TABLE dbo.Tasks
                                             ADD CONSTRAINT FK_Tasks_Sprints_SprintId
                                             FOREIGN KEY (SprintId) REFERENCES dbo.Sprints(Id);
                                         END;
                                         """;

        const string ensureUserFkSql = """
                                       IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                          AND OBJECT_ID('dbo.Users', 'U') IS NOT NULL
                                          AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Tasks_Users_AssignedUserId')
                                       BEGIN
                                           ALTER TABLE dbo.Tasks
                                           ADD CONSTRAINT FK_Tasks_Users_AssignedUserId
                                           FOREIGN KEY (AssignedUserId) REFERENCES dbo.Users(Id);
                                       END;
                                       """;

        const string ensureIndexesSql = """
                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_ProjectId' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_ProjectId ON dbo.Tasks(ProjectId);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_SprintId' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_SprintId ON dbo.Tasks(SprintId);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_SprintOrder' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_SprintOrder ON dbo.Tasks(SprintOrder);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_AssignedUserId' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_AssignedUserId ON dbo.Tasks(AssignedUserId);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_DueDate' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_DueDate ON dbo.Tasks(DueDate);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_Status' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_Status ON dbo.Tasks(Status);
                                        END;

                                        IF OBJECT_ID('dbo.Tasks', 'U') IS NOT NULL
                                           AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_Priority' AND object_id = OBJECT_ID('dbo.Tasks'))
                                        BEGIN
                                            CREATE INDEX IX_Tasks_Priority ON dbo.Tasks(Priority);
                                        END;
                                        """;

        await connection.ExecuteAsync(new CommandDefinition(createTableSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureProjectFkSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureSprintFkSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureUserFkSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ensureIndexesSql, cancellationToken: cancellationToken));
    }
}
