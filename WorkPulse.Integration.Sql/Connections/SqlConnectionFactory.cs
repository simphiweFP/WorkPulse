using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using WorkPulse.Application.Interfaces;

namespace WorkPulse.Integration.Sql.Connections;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
