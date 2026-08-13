using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Domain.Constants;

namespace WorkPulse.Infrastructure.Identity.Authorization;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddWorkPulseAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(Roles.Admin));
            options.AddPolicy(PolicyNames.AdminOrManager, policy => policy.RequireRole(Roles.Admin, Roles.Manager));
            options.AddPolicy(PolicyNames.AdminManagerEmployee, policy => policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Employee));
        });

        return services;
    }
}
