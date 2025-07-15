
using System.Collections.Concurrent;
using Application.AttachedResources;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources.Enums;
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
        List<Resources> resources = _resourceManager.GetResourcesByScheduleId(schedule.Id).Select(s=>s.ResourceType).ToList();
        await scheduleEventService.UpdateAsync(schedule,resources);
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