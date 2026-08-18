using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = FakeAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = FakeAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = FakeAuthenticationHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>(
                FakeAuthenticationHandler.SchemeName,
                _ => { });
        });
    }

    private sealed class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "FakeBearer";

        public FakeAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authorization = Request.Headers.Authorization.ToString();

            if (!authorization.StartsWith("Bearer fake-token|", StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var tokenParts = authorization["Bearer ".Length..].Split('|');

            if (tokenParts.Length != 3)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test token."));
            }

            var role = tokenParts[1];
            var userId = tokenParts[2];

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"{userId}@test.local"),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}