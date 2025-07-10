using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;

public interface IScheduleEventManager
{
    Task ExecuteAsync(ScheduleDto schedule, List<ScheduleResourceDto> resources);

    Task UpdateAsync(ScheduleDto schedule);
    Task DeleteAsync(Guid id);
    void executeLoadedTasks(ConcurrentDictionary<Guid, ScheduleDto> schedules);
    void UnscheduleJob(Guid id);
}