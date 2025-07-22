using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.AttachedResources;

public interface IResourceManager
{
    public List<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId);
    public bool IsResourceLoaded(Guid scheduleId);
    public int GetLoadedResourceCount();
    public List<ScheduleResourceDto> GetAllCachedResources();
    public Task RefreshCacheAsync();
    public Task InitializeAsync();
    protected Task LoadScheduleResourceMapping();
    public void RemoveFromMemory(Guid id);
    public Task AddScheduleResourceMap(ScheduleResourceDto map);
    public Task DeletScheduleResourceMap(List<Guid> ids,ScheduleAllDetails scheduleAllDetail);


}