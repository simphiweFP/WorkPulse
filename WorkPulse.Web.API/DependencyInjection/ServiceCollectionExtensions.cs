using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Web.API.Controllers;

namespace WorkPulse.Web.API.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkPulseWebApi(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        return services;
    }
}
