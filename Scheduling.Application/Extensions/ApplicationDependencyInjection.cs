using Application.AttachedResources.Service;
using Application.Schedule;
using Application.Schedule.ScheduleObj;
using Domain.AttachedResources;
using Domain.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.Schedule;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Extensions;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,IConfiguration configuration)
    {
       // Core Application Services
       services.AddSingleton<IScheduleManager, ScheduleManager>();
      
       services.AddSchedulingScheduler(configuration);
        // Auto-register services with attributes (your current approach)
        services.AddServicesOfType<IScopedService>();
        services.AddServicesWithAttributeOfType<ScopedServiceAttribute>();
        services.AddServicesOfType<ITransientService>();
        services.AddServicesWithAttributeOfType<TransientServiceAttribute>();
        services.AddServicesOfType<ISingletonService>();
        services.AddServicesWithAttributeOfType<SingletonServiceAttribute>();

        // MediatR
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(typeof(ApplicationDependencyInjection).Assembly));

        return services;
    }
}