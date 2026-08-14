using Dapper;
using Microsoft.Data.SqlClient;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Integration.Sql.Transactions;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private SqlConnection? _connection;

    public DapperUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        _connection ??= (SqlConnection)_connectionFactory.CreateConnection();
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await _connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await _connection.ExecuteAsync(new CommandDefinition("SET XACT_ABORT ON", transaction: transaction, cancellationToken: cancellationToken));
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        return _connection is null ? ValueTask.CompletedTask : _connection.DisposeAsync();
    }
}
