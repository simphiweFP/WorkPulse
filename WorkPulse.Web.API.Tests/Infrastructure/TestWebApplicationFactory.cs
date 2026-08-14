using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WorkPulse.Web.API.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var dbName = $"WorkPulseApiTests_{Guid.NewGuid():N}";
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
            };

            configBuilder.AddInMemoryCollection(settings);
        });
    }
}