using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkPulse.Integration.Identity.Models;
using WorkPulse.Integration.Identity.Services;

namespace WorkPulse.Web.API.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
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
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IIdentityService>();
            services.AddSingleton<IIdentityService, FakeIdentityService>();
        });
    }
}