using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkPulse.Integration.Sql.Migrations;

public sealed class DatabaseBootstrapper
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(IConfiguration configuration, ILogger<DatabaseBootstrapper> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        _logger = logger;
    }

    public async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(_connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("A database name must be configured in the DefaultConnection string.");
        }

        _logger.LogInformation("Checking database {DatabaseName}...", databaseName);

        var masterBuilder = new SqlConnectionStringBuilder(_connectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var existsCommand = new SqlCommand("SELECT DB_ID(@DatabaseName)", connection);
        existsCommand.Parameters.Add(new SqlParameter("@DatabaseName", SqlDbType.NVarChar, 128) { Value = databaseName });

        var existsResult = await existsCommand.ExecuteScalarAsync(cancellationToken);
        var exists = existsResult is not null && existsResult != DBNull.Value;
        if (exists)
        {
            _logger.LogInformation("Database {DatabaseName} already exists.", databaseName);
            return;
        }

        ValidateDatabaseName(databaseName);

        _logger.LogInformation("Database does not exist.");
        _logger.LogInformation("Creating database {DatabaseName}...", databaseName);

        var createCommand = new SqlCommand($"CREATE DATABASE {QuoteDatabaseName(databaseName)}", connection);
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Database created successfully.");
    }

    private static void ValidateDatabaseName(string databaseName)
    {
        if (databaseName.Length > 128)
        {
            throw new InvalidOperationException($"Database name '{databaseName}' is too long.");
        }

        foreach (var ch in databaseName)
        {
            if (char.IsControl(ch) || ch == ';' || ch == '\0')
            {
                throw new InvalidOperationException($"Database name '{databaseName}' contains invalid characters.");
            }
        }
    }

    private static string QuoteDatabaseName(string databaseName)
    {
        return new SqlCommandBuilder().QuoteIdentifier(databaseName);
    }
}
