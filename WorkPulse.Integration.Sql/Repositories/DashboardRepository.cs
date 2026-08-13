using Dapper;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Enums;
using TaskStatus = WorkPulse.Domain.Enums.TaskStatus;

namespace WorkPulse.Integration.Sql.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetDueTodayCountAsync(DateTime utcTodayStart, DateTime utcTomorrowStart, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE DueDate >= @TodayStart AND DueDate < @TomorrowStart";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { TodayStart = utcTodayStart, TomorrowStart = utcTomorrowStart }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetOverdueCountAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE DueDate < @Now AND Status <> @CompletedStatus";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Now = utcNow, CompletedStatus = (int)TaskStatus.Completed }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetCompletedCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE Status = @CompletedStatus";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { CompletedStatus = (int)TaskStatus.Completed }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetOpenCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE Status <> @CompletedStatus";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { CompletedStatus = (int)TaskStatus.Completed }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetHighPriorityCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE Priority IN (@High, @Critical)";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { High = (int)TaskPriority.High, Critical = (int)TaskPriority.Critical }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetActiveProjectsCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Projects WHERE Status = @ActiveStatus";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ActiveStatus = 1 }, cancellationToken: cancellationToken));
    }

    public async Task<int> GetMyAssignedTasksCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT COUNT(1) FROM Tasks WHERE AssignedUserId = @UserId";
        await using var connection = (Microsoft.Data.SqlClient.SqlConnection)_connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
    }
}
