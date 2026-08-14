using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WorkPulse.Web.Main;

namespace WorkPulse.Web.Main.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<HostEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        var secret = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        builder.UseSetting("Jwt:SecretKey", secret);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = secret
            });
        });
    }
}
