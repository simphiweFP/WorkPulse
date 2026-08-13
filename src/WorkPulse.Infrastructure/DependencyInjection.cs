using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WorkPulse.Infrastructure.Identity.Authentication;
using WorkPulse.Infrastructure.Identity.Authorization;
using WorkPulse.Infrastructure.Identity.CurrentUser;
using WorkPulse.Infrastructure.Identity.Jwt;
using WorkPulse.Infrastructure.Identity.Passwords;

namespace WorkPulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT settings are missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey))
                };
            });

        services.AddWorkPulseAuthorizationPolicies();
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
