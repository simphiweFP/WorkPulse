using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WorkPulse.Web.Main;

namespace WorkPulse.Web.Main.Tests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<HostEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
