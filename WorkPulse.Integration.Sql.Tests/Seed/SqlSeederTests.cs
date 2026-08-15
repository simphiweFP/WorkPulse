using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkPulse.Application.Interfaces;
using WorkPulse.Integration.Sql.Seed;

namespace WorkPulse.Integration.Sql.Tests.Seed;

public class SqlSeederTests
{
    [Fact]
    public async Task SeedAsync_WithIdentityTablesOnly_SeedsIdentityAndSkipsSampleData()
    {
        var databaseName = $"WorkPulseSeedIdentityOnly_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        try
        {
            await CreateIdentityTablesAsync(databaseName);

            var seeder = CreateSeeder(databaseName, seedSampleData: true);
            await seeder.SeedAsync(CreateLogger());

            await using var connection = await OpenDatabaseConnectionAsync(databaseName);
            var roleCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Roles");
            var userCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users");
            var clientsExists = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' AND TABLE_NAME='Clients'");

            Assert.True(roleCount >= 2);
            Assert.True(userCount >= 5);
            Assert.Equal(0, clientsExists);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task SeedAsync_WithAllRequiredTables_SeedsSampleData_Idempotently()
    {
        var databaseName = $"WorkPulseSeedFull_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        try
        {
            await CreateIdentityTablesAsync(databaseName);
            await CreateSampleDataTablesAsync(databaseName);

            var seeder = CreateSeeder(databaseName, seedSampleData: true);
            var logger = CreateLogger();

            await seeder.SeedAsync(logger);
            var firstCounts = await GetCountsAsync(databaseName);

            await seeder.SeedAsync(logger);
            var secondCounts = await GetCountsAsync(databaseName);

            Assert.Equal(5, firstCounts.Users);
            Assert.Equal(2, firstCounts.Roles);
            Assert.Equal(5, firstCounts.Clients);
            Assert.Equal(9, firstCounts.Projects);
            Assert.Equal(24, firstCounts.Tasks);

            Assert.Equal(firstCounts, secondCounts);
        }
        finally
        {
            await DropDatabaseAsync(databaseName);
        }
    }

    private static SqlSeeder CreateSeeder(string databaseName, bool seedSampleData)
    {
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        var connectionFactory = new TestConnectionFactory(connectionString);
        var options = Options.Create(new DevelopmentSeedOptions
        {
            Enabled = true,
            AdminEmail = "admin@workpulse.local",
            AdminPassword = "WorkPulseAdmin123!",
            SeedSampleData = seedSampleData
        });

        return new SqlSeeder(connectionFactory, new TestPasswordHasher(), options);
    }

    private static ILogger CreateLogger()
        => LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None)).CreateLogger("SqlSeederTests");

    private static async Task<(int Users, int Roles, int Clients, int Projects, int Tasks)> GetCountsAsync(string databaseName)
    {
        await using var connection = await OpenDatabaseConnectionAsync(databaseName);
        var users = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users");
        var roles = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Roles");
        var clients = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Clients");
        var projects = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Projects");
        var tasks = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Tasks");
        return (users, roles, clients, projects, tasks);
    }

    private static async Task CreateDatabaseAsync(string databaseName)
    {
        await using var connection = await OpenMasterConnectionAsync();
        await connection.ExecuteAsync($"CREATE DATABASE [{databaseName}]");
    }

    private static async Task DropDatabaseAsync(string databaseName)
    {
        await using var connection = await OpenMasterConnectionAsync();
        var sql = $"""
                   IF DB_ID('{databaseName}') IS NOT NULL
                   BEGIN
                       ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                       DROP DATABASE [{databaseName}];
                   END
                   """;
        await connection.ExecuteAsync(sql);
    }

    private static async Task CreateIdentityTablesAsync(string databaseName)
    {
        await using var connection = await OpenDatabaseConnectionAsync(databaseName);
        const string sql = """
                           CREATE TABLE dbo.Users (
                               Id NVARCHAR(64) NOT NULL PRIMARY KEY,
                               FirstName NVARCHAR(100) NOT NULL,
                               LastName NVARCHAR(100) NOT NULL,
                               Email NVARCHAR(256) NOT NULL,
                               UserName NVARCHAR(256) NOT NULL,
                               PasswordHash NVARCHAR(MAX) NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               IsDeleted BIT NOT NULL DEFAULT 0
                           );

                           CREATE TABLE dbo.Roles (
                               Id INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
                               Name NVARCHAR(50) NOT NULL
                           );

                           CREATE TABLE dbo.UserRoles (
                               UserId NVARCHAR(64) NOT NULL,
                               RoleId INT NOT NULL,
                               CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId)
                           );
                           """;
        await connection.ExecuteAsync(sql);
    }

    private static async Task CreateSampleDataTablesAsync(string databaseName)
    {
        await using var connection = await OpenDatabaseConnectionAsync(databaseName);
        const string sql = """
                           CREATE TABLE dbo.Clients (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               Name NVARCHAR(200) NOT NULL,
                               ContactName NVARCHAR(200) NOT NULL,
                               ContactEmail NVARCHAR(256) NOT NULL,
                               PhoneNumber NVARCHAR(50) NOT NULL,
                               Description NVARCHAR(1000) NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               IsDeleted BIT NOT NULL DEFAULT 0
                           );

                           CREATE TABLE dbo.Projects (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               ClientId UNIQUEIDENTIFIER NOT NULL,
                               Name NVARCHAR(200) NOT NULL,
                               Description NVARCHAR(2000) NOT NULL,
                               StartDate DATETIME2 NOT NULL,
                               EndDate DATETIME2 NULL,
                               Status INT NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL
                           );

                           CREATE TABLE dbo.Tasks (
                               Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                               ProjectId UNIQUEIDENTIFIER NOT NULL,
                               AssignedUserId NVARCHAR(64) NULL,
                               Title NVARCHAR(200) NOT NULL,
                               Description NVARCHAR(2000) NOT NULL,
                               DueDate DATETIME2 NULL,
                               Status INT NOT NULL,
                               Priority INT NOT NULL,
                               CreatedAt DATETIME2 NOT NULL,
                               UpdatedAt DATETIME2 NOT NULL,
                               CompletedAt DATETIME2 NULL
                           );
                           """;
        await connection.ExecuteAsync(sql);
    }

    private static async Task<SqlConnection> OpenMasterConnectionAsync()
    {
        var connection = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<SqlConnection> OpenDatabaseConnectionAsync(string databaseName)
    {
        var connection = new SqlConnection($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        await connection.OpenAsync();
        return connection;
    }

    private sealed class TestConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public TestConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed::{password}";
        public bool Verify(string password, string hash) => hash == Hash(password);
    }
}
