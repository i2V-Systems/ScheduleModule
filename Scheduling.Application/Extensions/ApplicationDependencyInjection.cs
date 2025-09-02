using System.Reflection;
using Application.AttachedResources;
using Application.AttachedResources.Service;
using Application.Schedule;
using Application.Schedule.ScheduleEvent;
using Application.Schedule.ScheduleEvent.JobKey;
using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Application.Schedule.ScheduleEvent.Validator;
using Application.Schedule.ScheduleObj;
using Domain.AttachedResources;
using Domain.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Extensions;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,IConfiguration configuration)
    { 
       
     
       // Core Application Services
       services.AddTransient<IScheduleEventService,ScheduleEventService>();
       services.AddSingleton<IResourceManager, ResourceManager>();
       services.AddHostedService<ResourceInitializationService>();
       services.AddTransient<IScheduleValidator, ScheduleValidator>();
       services.AddTransient<IScheduleStrategyFactory, ScheduleStrategyFactory>();
       services.AddTransient<IJobKeyGenerator, JobKeyGenerator>();
       services.AddSingleton<IScheduleEventManager, ScheduleEventManager>();
       services.AddSingleton<IScheduleManager, ScheduleManager>();
       services.AddHostedService<ScheduleInitializationService>();
       
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
            cfg.RegisterServicesFromAssembly(
                Assembly.Load(new AssemblyName("Scheduling.Application")))
            );

        return services;
    }
}