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

        var hasSprintSupport = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Tasks' AND COLUMN_NAME = 'SprintId'",
            cancellationToken: cancellationToken)) > 0;

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

                if (!hasSprintSupport)
                {
                    _logger.LogInformation("Patching existing schema with sprint support...");
                    await EnsureSprintSupportAsync(sqlConnection, cancellationToken);
                    _logger.LogInformation("Sprint support patch completed successfully.");
                }

                return;
            }

            if (existingCoreTables > 0)
            {
                _logger.LogWarning("Skipping FluentMigrator because a partial schema already exists without VersionInfo. Recreate the local database for a clean migration run.");
                return;
            }
        }

        if (hasVersionInfo && !hasSprintSupport)
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

        _logger.LogInformation("Running FluentMigrator migrations...");
        _migrationRunner.MigrateUp();
        _logger.LogInformation("Migrations completed successfully.");
    }

    private static async Task EnsureSprintSupportAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
                           IF COL_LENGTH('dbo.Tasks', 'SprintId') IS NULL
                           BEGIN
                               ALTER TABLE dbo.Tasks ADD SprintId UNIQUEIDENTIFIER NULL;
                           END;

                           IF OBJECT_ID('dbo.Sprints', 'U') IS NULL
                           BEGIN
                               CREATE TABLE dbo.Sprints (
                                   Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                                   Name NVARCHAR(200) NOT NULL,
                                   StartDate DATETIME2 NOT NULL,
                                   EndDate DATETIME2 NOT NULL,
                                   Status INT NOT NULL,
                                   CreatedAt DATETIME2 NOT NULL,
                                   UpdatedAt DATETIME2 NOT NULL
                               );
                           END;

                           IF OBJECT_ID('dbo.FK_Tasks_Sprints_SprintId', 'F') IS NULL
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
                           """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
