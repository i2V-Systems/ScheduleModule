using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;

public interface IScheduleEventManager
{
    Task ExecuteAsync(ScheduleDto schedule);

    Task UpdateAsync(ScheduleDto schedule);
    Task DeleteAsync(Guid id);
    void executeLoadedTasks(ConcurrentDictionary<Guid, ScheduleDto> schedules);
}