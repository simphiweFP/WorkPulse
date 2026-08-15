using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WorkPulse.Application.Common.Exceptions;
using WorkPulse.Application.DTOs.Clients;
using WorkPulse.Application.Interfaces;
using WorkPulse.Domain.Enums;
using WorkPulse.Integration.Identity.Roles;
using WorkPulse.Web.API.Tests.Infrastructure;

namespace WorkPulse.Web.API.Tests.Controllers;

public class ControllerPipelineTests
{
    [Fact]
    public async Task Projects_GetAll_ShouldRequireAuthentication()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Clients_GetAll_ShouldReturnForbidden_ForDeveloperUser()
    {
        await using var factory = new AuthenticatedTestWebApplicationFactory([WorkPulseRoles.Developer]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.UserIdHeader, "dev-1");
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.RolesHeader, WorkPulseRoles.Developer);

        var response = await client.GetAsync("/api/clients");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Clients_Create_ShouldReturnBadRequest_ForInvalidPayload()
    {
        await using var factory = new AuthenticatedTestWebApplicationFactory([WorkPulseRoles.Admin]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.UserIdHeader, "admin-1");
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.RolesHeader, WorkPulseRoles.Admin);

        var response = await client.PostAsJsonAsync("/api/clients", new
        {
            contactName = "Contact",
            contactEmail = "client@workpulse.local",
            phoneNumber = "555-0100",
            description = "Missing required name"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Clients_GetById_ShouldReturnNotFound_WhenServiceThrows()
    {
        await using var factory = new AuthenticatedTestWebApplicationFactory([WorkPulseRoles.Admin]);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.UserIdHeader, "admin-1");
        client.DefaultRequestHeaders.Add(AuthenticatedTestWebApplicationFactory.RolesHeader, WorkPulseRoles.Admin);

        var response = await client.GetAsync($"/api/clients/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Client not found.", payload);
    }

    private sealed class AuthenticatedTestWebApplicationFactory : TestWebApplicationFactory
    {
        public const string UserIdHeader = "X-Test-UserId";
        public const string RolesHeader = "X-Test-Roles";
        private const string SchemeName = "TestScheme";

        public AuthenticatedTestWebApplicationFactory(string[] roles)
        {
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = SchemeName;
                        options.DefaultChallengeScheme = SchemeName;
                        options.DefaultScheme = SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(SchemeName, _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = SchemeName;
                    options.DefaultChallengeScheme = SchemeName;
                    options.DefaultScheme = SchemeName;
                });

                services.RemoveAll<IClientService>();
                services.AddSingleton<IClientService>(new ThrowingClientService());
            });
        }

        private sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
        {
            public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, UrlEncoder encoder)
                : base(options, logger, encoder)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                var userId = Request.Headers[UserIdHeader].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    userId = "test-user";
                }

                var roles = Request.Headers[RolesHeader].FirstOrDefault();
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, userId)
                };

                var effectiveRoles = string.IsNullOrWhiteSpace(roles)
                    ? [WorkPulseRoles.Developer]
                    : roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var role in effectiveRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        private sealed class ThrowingClientService : IClientService
        {
            public Task<IReadOnlyCollection<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyCollection<ClientDto>>([]);

            public Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromException<ClientDto>(new NotFoundException("Client not found."));

            public Task<ClientDto> CreateAsync(CreateClientRequestDto request, CancellationToken cancellationToken = default)
                => Task.FromResult(new ClientDto
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    ContactName = request.ContactName,
                    ContactEmail = request.ContactEmail,
                    PhoneNumber = request.PhoneNumber,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

            public Task UpdateAsync(Guid id, UpdateClientRequestDto request, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }
}
