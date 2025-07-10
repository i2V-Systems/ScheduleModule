using System.Reflection;
using Application.Extensions;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Extensions;

namespace Presentation;

public enum SchedulerType
{
    Coravel,
    Hangfire
}

public static class ScheduleModuleRegistration
{
    public static SchedulerType CurrentSchedulerType { get; private set; }
    
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // // Set current scheduler type based on configuration
        // var schedulingService = configuration.GetValue<string>("SchedulingService", "coravel");
        // CurrentSchedulerType = schedulingService.ToLowerInvariant() switch
        // {
        //     "coravel" => SchedulerType.Coravel,
        //     "hangfire" => SchedulerType.Hangfire,
        //     _ => SchedulerType.Coravel
        // };
        // var scheduleAssembly = Assembly.Load("Scheduling.Presentation");
        // services.AddMvc()
        //     .AddApplicationPart(scheduleAssembly)
        //     .AddControllersAsServices();
        // // Register services in logical order
        // services.AddPresentationServices();

        return services;
    }
    
    public static void ConfigureSchedulingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices(configuration);
        
        var scheduleAssembly = Assembly.Load("Scheduling.Presentation");
        services.AddControllers()
            .AddApplicationPart(scheduleAssembly)
            .AddControllersAsServices();
       
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(scheduleAssembly)
        );
    }
    
}