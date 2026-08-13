using Microsoft.AspNetCore.Mvc.Testing;

namespace WorkPulse.Web.Main.Tests.Smoke;

public class HostSmokeTests
{
    [Fact]
    public async Task Host_ShouldStartAndCreateClient()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
