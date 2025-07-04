
using System.Reflection;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Schedule.ScheduleEvent;

public  class ScheduleEventManager
{
    private readonly IConfiguration _configuration;
    public static ISchedulerService scheduleEventService { get; private set; }
    internal static void Init(IServiceProvider serviceProvider,IConfiguration configuration)
    {
        string serviceName= configuration.GetValue<string>("SchedulingService") ??  throw new InvalidOperationException("SchedulingService configuration value is missing or empty.");;
        
        scheduleEventService = ResolveService(serviceProvider, serviceName)
                               ?? throw new InvalidOperationException("Unable to resolve the scheduler task service.");
    }
 
    private static ISchedulerService ResolveService(IServiceProvider serviceProvider, string serviceName)
    {
        var targetType = typeof(ISchedulerService);
        var matchingType = Assembly.GetExecutingAssembly()
            .GetTypes()
            .FirstOrDefault(t =>
                t.IsClass &&
                !t.IsAbstract &&
                targetType.IsAssignableFrom(t) &&
                t.Name.ToLower().Contains(serviceName));

        if (matchingType != null)
        {
            return CreateInstance(matchingType, serviceProvider);
        }
        // fallback to default implementation
        return serviceProvider.GetService<CoravelSchedulerService>();
    }
    
    private static ISchedulerService CreateInstance(Type serviceType, IServiceProvider serviceProvider)
    {
        try
        {
            // get from DI container 
            var serviceFromDi = serviceProvider.GetRequiredService(serviceType);
            if (serviceFromDi != null)
            {
                return (ISchedulerService)serviceFromDi;
            }
            return serviceProvider.GetRequiredService<CoravelSchedulerService>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of {serviceType.Name}: {ex.Message}", ex);
        }
    }
    
    
}