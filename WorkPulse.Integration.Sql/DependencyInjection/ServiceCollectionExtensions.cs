using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentMigrator.Runner;
using WorkPulse.Application.Interfaces;
using WorkPulse.Integration.Sql.Connections;
using WorkPulse.Integration.Sql.Repositories;
using WorkPulse.Integration.Sql.Seed;
using WorkPulse.Integration.Sql.Transactions;

namespace WorkPulse.Integration.Sql.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkPulseSqlIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.Configure<DevelopmentSeedOptions>(configuration.GetSection(DevelopmentSeedOptions.SectionName));

        services
            .AddFluentMigratorCore()
            .ConfigureRunner(runner => runner
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(ServiceCollectionExtensions).Assembly).For.Migrations())
            .AddLogging(logging => logging.AddFluentMigratorConsole());

        services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<SqlSeeder>();

        return services;
    }
}
