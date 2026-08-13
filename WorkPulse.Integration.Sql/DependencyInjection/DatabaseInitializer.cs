using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkPulse.Integration.Sql.Seed;

namespace WorkPulse.Integration.Sql.DependencyInjection;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var seeder = scope.ServiceProvider.GetRequiredService<SqlSeeder>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("WorkPulseSeeder");

        migrationRunner.MigrateUp();
        await seeder.SeedAsync(logger, cancellationToken);
    }
}
