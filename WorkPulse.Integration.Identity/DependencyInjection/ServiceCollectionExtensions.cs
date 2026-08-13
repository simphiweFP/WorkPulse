using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Integration.Identity.Authentication;
using WorkPulse.Integration.Identity.Services;

namespace WorkPulse.Integration.Identity.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkPulseIdentityIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IIdentityService, IdentityService>();

        return services;
    }
}
