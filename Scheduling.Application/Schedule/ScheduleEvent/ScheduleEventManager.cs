
using System.Collections.Concurrent;
using System.Reflection;
using Application.AttachedResources;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Application.Schedule.ScheduleObj;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.Schedule.ScheduleEvent;

internal  class ScheduleEventManager
{

    private static  ResourceManager _resourceManager;
    private static IServiceProvider _serviceProvider;

    ScheduleEventManager( ResourceManager resourceManager,IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _resourceManager = resourceManager;
    }
    private static readonly IConfiguration _configuration;
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

    public  static async Task ExecuteAsync(ScheduleDto schedule, IUnifiedScheduler _scheduler,    List<ScheduleResourceDto> resources )
    {
        var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>(); 
        await scheduleEventService.ExecuteAsync(schedule, _scheduler, resources);
    }

    public static async Task UpdateAsync(ScheduleDto schedule)
    {
        var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>(); 
        await scheduleEventService.UpdateAsync(schedule);
    }
    public static async Task DeleteAsync(Guid id)
    {
        var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>(); 
        await scheduleEventService.DeleteAsync(id);
    }
    public static void executeLoadedTasks( ConcurrentDictionary<Guid, ScheduleDto> schedules,  IUnifiedScheduler scheduler)
    {
        var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>(); 
         schedules.Select(item =>
        {
            List<ScheduleResourceDto> resources = _resourceManager.GetResourcesByScheduleId(item.Value.Id).ToList();
            return scheduleEventService.ExecuteAsync(item.Value, scheduler, resources);
        });
      
    }
    
}