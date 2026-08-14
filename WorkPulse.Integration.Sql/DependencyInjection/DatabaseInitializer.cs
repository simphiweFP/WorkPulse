using FluentMigrator.Runner;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkPulse.Application.Interfaces;
using WorkPulse.Integration.Sql.Seed;

namespace WorkPulse.Integration.Sql.DependencyInjection;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("WorkPulseSeeder");

        using var connection = connectionFactory.CreateConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        var hasVersionInfo = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'VersionInfo'") > 0;

        var hasCoreTables = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('Users', 'Clients', 'Projects', 'Tasks')") >= 4;

        if (!hasCoreTables)
        {
            migrationRunner.MigrateUp();
        }
        else
        {
            logger.LogInformation("Skipping migrations because core tables already exist.");
        }

        await seeder.SeedAsync(logger, cancellationToken);
    }
}
