
using System.Collections.Concurrent;
using System.Reflection;
using Application.AttachedResources;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Serilog;

namespace Application.Schedule.ScheduleEvent;

internal class ScheduleEventManager :IScheduleEventManager
{

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    
    private readonly  ResourceManager _resourceManager;
    
    public ScheduleEventManager(IServiceProvider serviceProvider,IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _resourceManager = new ResourceManager(configuration,serviceProvider);
    }
 
    
    public   async Task ExecuteAsync(ScheduleDto schedule, List<ScheduleResourceDto> resources )
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduledEventService>();
        await scheduleEventService.ExecuteAsync(schedule, resources);
    }

    public async Task UpdateAsync(ScheduleDto schedule)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduledEventService>();
        await scheduleEventService.UpdateAsync(schedule);
    }
    public async Task DeleteAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduledEventService>();
        await scheduleEventService.DeleteAsync(id);
    }
    public void executeLoadedTasks( ConcurrentDictionary<Guid, ScheduleDto> schedules)
    {
        try
        {
            var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>();
            schedules.Select(item =>
            {
                List<ScheduleResourceDto> resources = _resourceManager.GetResourcesByScheduleId(item.Value.Id).ToList();
                return scheduleEventService.ExecuteAsync(item.Value, resources);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        
      
    }
    public void UnscheduleJob(Guid id)
    {
        var scheduleEventService = _serviceProvider.GetRequiredService<ScheduledEventService>();
        scheduleEventService.UnscheduleJob(id);
    }
}