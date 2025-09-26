
using System.Collections.Concurrent;
using Application.AttachedResources;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Serilog;

namespace Application.Schedule.ScheduleEvent;

internal class ScheduleEventManager :IScheduleEventManager
{

    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    
    private readonly  IResourceManager _resourceManager;
    
    public ScheduleEventManager(IServiceProvider serviceProvider,IConfiguration configuration,IResourceManager resourceManager)
    {
        _serviceProvider = serviceProvider;
        _resourceManager = resourceManager;
    }
 
    
    public   async Task ExecuteAsync(ScheduleDto schedule )
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduleEventService>();
        List<Resources> resources = _resourceManager.GetResourcesByScheduleId(schedule.Id).Select(s=>s.ResourceType).ToList();
        await scheduleEventService.ExecuteAsync(schedule,resources);
    }

    public async Task UpdateAsync(ScheduleDto schedule)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduleEventService>();
        
        List<Resources> resources = _resourceManager.GetResourcesByScheduleId(schedule.Id)
            .Select(s=>s.ResourceType).ToList();
        
        var scheduleExists= await scheduleEventService.ScheduleExistsAsync(schedule.Id);
        if (!scheduleExists)
        {
            await scheduleEventService.ExecuteAsync(schedule, resources);
        }
        else
        {
            await scheduleEventService.UpdateAsync(schedule, resources);
        
            // Handle enable/disable based on desired status
            if (schedule.Status == ScheduleStatus.Disabled)
            {
                await scheduleEventService.DisableAsync(schedule.Id);
            }
            else if (schedule.Status == ScheduleStatus.Enabled)
            {
                await scheduleEventService.EnableAsync(schedule.Id);
            }
        }
    }
    public async Task DeleteAsync(Guid id)
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleEventService = scope.ServiceProvider.GetRequiredService<ScheduleEventService>();
        await scheduleEventService.DeleteAsync(id);
    }
    public void executeLoadedTasks( ConcurrentDictionary<Guid, ScheduleDto> schedules)
    {
        try
        {
            var scheduleEventService = _serviceProvider.GetRequiredService<ScheduleEventService>();
            schedules.Select(item =>
            {
                List<Resources> resources = _resourceManager.GetResourcesByScheduleId(item.Value.Id).Select(s=>s.ResourceType).ToList();
                return scheduleEventService.ExecuteAsync(item.Value, resources);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
        
      
    }
}