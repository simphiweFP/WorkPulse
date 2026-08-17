using System.Net;
using System.Net.Http.Json;
using WorkPulse.Web.API.Contracts.Responses;
using WorkPulse.Web.API.Tests.Infrastructure;

namespace WorkPulse.Web.API.Tests.Users;

public class UsersEndpointsTests
{
    [Fact]
    public async Task GetUsers_Unauthenticated_ShouldReturnUnauthorized()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ShouldReturnOk()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // login as seeded admin
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@workpulse.local", password = "WorkPulseAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<UserResponse[]>();
        Assert.NotNull(users);
        Assert.True(users.Length >= 1);
    }

    [Fact]
    public async Task PatchRole_InvalidRole_ShouldReturnBadRequest()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // register a new user to manage
        var email = $"user{Guid.NewGuid():N}@example.com";
        var register = new { firstName = "Test", lastName = "User", email, password = "ValidPass123", confirmPassword = "ValidPass123" };
        var regResp = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);
        var reg = await regResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(reg);

        // login as admin
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@workpulse.local", password = "WorkPulseAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

        var patchResp = await client.PatchAsJsonAsync($"/api/users/{reg.User.Id}/role", new { role = "SuperUser" });
        Assert.Equal(HttpStatusCode.BadRequest, patchResp.StatusCode);
    }

    [Fact]
    public async Task PatchRole_AdminCannotChangeOwnRole_ShouldReturnBadRequest()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        // login as admin
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@workpulse.local", password = "WorkPulseAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

        var patchResp = await client.PatchAsJsonAsync($"/api/users/{auth.User.Id}/role", new { role = "Pending" });
        Assert.Equal(HttpStatusCode.BadRequest, patchResp.StatusCode);
    }
}
