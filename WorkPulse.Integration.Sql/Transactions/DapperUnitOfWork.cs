using Dapper;
using Microsoft.Data.SqlClient;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Integration.Sql.Transactions;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DapperUnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        using var connection = (SqlConnection)_connectionFactory.CreateConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await connection.ExecuteAsync(new CommandDefinition("SET XACT_ABORT ON", transaction: transaction, cancellationToken: cancellationToken));
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
