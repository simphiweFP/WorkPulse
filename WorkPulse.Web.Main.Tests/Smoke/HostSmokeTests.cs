using Microsoft.AspNetCore.Mvc.Testing;
using WorkPulse.Web.Main.Tests.Infrastructure;

namespace WorkPulse.Web.Main.Tests.Smoke;

public class HostSmokeTests
{
    [Fact]
    public async Task Host_ShouldStartAndCreateClient()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
