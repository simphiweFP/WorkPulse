using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkPulse.Domain.Entities;
using WorkPulse.Integration.Sql.Context;

namespace WorkPulse.Integration.Sql.Tests.Context;

public class WorkPulseDbContextTests
{
    [Fact]
    public async Task CanSaveAndLoadClientEntity()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<WorkPulseDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new WorkPulseDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Acme",
            ContactName = "Jane Doe",
            ContactEmail = "jane@acme.example",
            PhoneNumber = "123",
            Description = "Test client"
        };

        context.Clients.Add(client);
        await context.SaveChangesAsync();

        var saved = await context.Clients.SingleAsync();
        Assert.Equal("Acme", saved.Name);
    }
}
