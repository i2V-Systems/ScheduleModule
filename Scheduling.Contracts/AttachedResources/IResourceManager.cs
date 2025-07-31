using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.AttachedResources;

public interface IResourceManager
{
    public List<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId);
    public event EventHandler<List<ScheduleResourceDto>> ScheduleResourcePublish;

    public bool IsResourceLoaded(Guid scheduleId);
    public int GetLoadedResourceCount();
    public List<ScheduleResourceDto> GetAllCachedResources();
    public Task RefreshCacheAsync();
    public Task InitializeAsync();
    protected Task LoadScheduleResourceMapping();
    public void RemoveFromMemory(Guid id);
    public Task AddScheduleResourceMap(ScheduleResourceDto map);
    public Task DeleteScheduleResourceMap(List<Guid> ids,bool Notify=false);
    public List<ScheduleResourceDto> GetScheduleMappingsByResource(Guid resourceId, Resources resourceType);


}