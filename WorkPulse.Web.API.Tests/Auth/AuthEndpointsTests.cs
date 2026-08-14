using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WorkPulse.Web.API.Tests.Infrastructure;

namespace WorkPulse.Web.API.Tests.Auth;

public class AuthEndpointsTests
{
    [Fact]
    public async Task Register_ShouldCreateDeveloperUser()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            firstName = "John",
            lastName = "Dev",
            email = $"john{Guid.NewGuid():N}@example.com",
            password = "ValidPass123",
            confirmPassword = "ValidPass123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Developer", json.GetProperty("user").GetProperty("role").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Register_ShouldRejectDuplicateEmail()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var email = $"dup{Guid.NewGuid():N}@example.com";

        var payload = new
        {
            firstName = "Jane",
            lastName = "Dup",
            email,
            password = "ValidPass123",
            confirmPassword = "ValidPass123"
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", payload);
        var second = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldRejectInvalidPassword()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            firstName = "Amy",
            lastName = "Weak",
            email = $"weak{Guid.NewGuid():N}@example.com",
            password = "short",
            confirmPassword = "short"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldRejectMismatchedPasswords()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            firstName = "Mia",
            lastName = "Mismatch",
            email = $"mismatch{Guid.NewGuid():N}@example.com",
            password = "ValidPass123",
            confirmPassword = "DifferentPass123"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicRegistration_ShouldNeverCreateAdmin()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var payload = new
        {
            firstName = "Role",
            lastName = "Bypass",
            email = $"role{Guid.NewGuid():N}@example.com",
            password = "ValidPass123",
            confirmPassword = "ValidPass123",
            role = "Admin",
            roles = new[] { "Admin" },
            isAdmin = true
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Developer", json.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_ShouldReturnTokenForValidCredentials()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var email = $"login{Guid.NewGuid():N}@example.com";
        var register = new
        {
            firstName = "Login",
            lastName = "User",
            email,
            password = "ValidPass123",
            confirmPassword = "ValidPass123"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", register);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "ValidPass123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Developer", json.GetProperty("user").GetProperty("role").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Login_ShouldRejectInvalidCredentials()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "developer@workpulse.local", password = "WrongPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ShouldRequireAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}