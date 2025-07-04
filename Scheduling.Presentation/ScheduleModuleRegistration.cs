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
        // Set current scheduler type based on configuration
        var schedulingService = configuration.GetValue<string>("SchedulingService", "coravel");
        CurrentSchedulerType = schedulingService.ToLowerInvariant() switch
        {
            "coravel" => SchedulerType.Coravel,
            "hangfire" => SchedulerType.Hangfire,
            _ => SchedulerType.Coravel
        };

        // Register services in logical order
        services.AddInfrastructureServices(configuration);
        services.AddApplicationServices(configuration);
        services.AddPresentationServices();

        return services;
    }

    
}