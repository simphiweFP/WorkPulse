using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Web.API.Controllers;

namespace WorkPulse.Web.API.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkPulseWebApi(this IServiceCollection services)
    {
        var mvcBuilder = services.AddControllers();
        mvcBuilder.AddApplicationPart(typeof(AuthController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(ClientsController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(ProjectsController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(TasksController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(SprintsController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(TodayController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(DashboardController).Assembly);
        mvcBuilder.AddApplicationPart(typeof(UsersController).Assembly);
        return services;
    }
}
