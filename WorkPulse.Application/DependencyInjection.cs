using Microsoft.Extensions.DependencyInjection;
using WorkPulse.Application.Interfaces;
using WorkPulse.Application.Services;
using WorkPulse.Domain.Services;

namespace WorkPulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, WorkPulse.Application.Services.SystemClock>();
        services.AddScoped<WorkPulse.Application.Interfaces.IClientService, WorkPulse.Application.Services.ClientService>();
        services.AddScoped<IProjectService, WorkPulse.Application.Services.ProjectService>();
        services.AddScoped<ITaskService, WorkPulse.Application.Services.TaskService>();
        services.AddScoped<ITodayService, WorkPulse.Application.Services.TodayService>();
        services.AddScoped<ITodayTaskService, TodayTaskService>();

        return services;
    }
}