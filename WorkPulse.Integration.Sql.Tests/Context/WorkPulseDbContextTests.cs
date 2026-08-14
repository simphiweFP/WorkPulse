using Microsoft.Extensions.Configuration;
using WorkPulse.Integration.Sql.Connections;

namespace WorkPulse.Integration.Sql.Tests.Context;

public class WorkPulseDbContextTests
{
    [Fact]
    public void SqlConnectionFactory_ShouldCreateSqlConnection()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=WorkPulseDbTests;Trusted_Connection=True;"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var factory = new SqlConnectionFactory(configuration);
        using var connection = factory.CreateConnection();

        Assert.Equal("SqlConnection", connection.GetType().Name);
    }
}
