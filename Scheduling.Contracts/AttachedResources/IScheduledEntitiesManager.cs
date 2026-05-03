using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.AttachedResources;

public interface IScheduledEntitiesManager
{
    public List<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId);
    public event EventHandler<ScheduleResourceDto> ScheduleResourcePublish;

    public List<ScheduleResourceDto> GetAllCachedResources();
    public Task InitializeAsync();
    public void RemoveFromMemorywithScheduleId(Guid scheduleId);
    public Task AddScheduleResourceMap(ScheduleResourceDto map);
    public Task UpdateScheduleResourceMap(ScheduleResourceDto map);
    public Task<Guid> DeleteScheduleResourceMap(Guid id, bool Notify = false);
    public Task<ScheduleAllDetails> DeleteMultipleScheduleResourceMap(List<Guid> id, ScheduleAllDetails scheduleAllDetails);
}
