using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.AttachedResources;

public interface IResourceManager
{
    public List<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId);
    public event EventHandler<ScheduleResourceDto> ScheduleResourcePublish;

    public bool IsResourceLoaded(Guid scheduleId);
    public int GetLoadedResourceCount();
    public List<ScheduleResourceDto> GetAllCachedResources();
    public Task RefreshCacheAsync();
    public Task InitializeAsync();
    protected Task LoadScheduleResourceMapping();
    public void RemoveFromMemory(Guid id);
    public void RemoveFromMemorywithScheduleId(Guid scheduleId);
    public Task AddScheduleResourceMap(ScheduleResourceDto map);
    public Task UpdateScheduleResourceMap(ScheduleResourceDto map);
    public Task<Guid> DeleteScheduleResourceMap(Guid id,bool Notify=false);
    public Task<ScheduleAllDetails> DeleteMultipleScheduleResourceMap(List<Guid> id,ScheduleAllDetails scheduleAllDetails);


}