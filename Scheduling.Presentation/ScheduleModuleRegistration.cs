using System.Reflection;
using Application.Extensions;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Extensions;
using Scheduling.Contracts.Extensions;

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
        return services;
    }
    
    public static void ConfigureSchedulingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddContractServices(configuration);
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