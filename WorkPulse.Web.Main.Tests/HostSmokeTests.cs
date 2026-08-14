using Microsoft.AspNetCore.Mvc.Testing;
using WorkPulse.Web.Main.Tests.Infrastructure;

namespace WorkPulse.Web.Main.Tests;

public class HostSmokeTests
{
    [Fact]
    public async Task Host_ShouldStart_And_Return401ForProtectedAuthMe()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
