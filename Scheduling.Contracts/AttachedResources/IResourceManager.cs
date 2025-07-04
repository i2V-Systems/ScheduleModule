using Scheduling.Contracts.AttachedResources.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.AttachedResources;

[SingletonService]

public interface IResourceManager
{
    public IEnumerable<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId);
    public bool IsResourceLoaded(Guid scheduleId);
    public int GetLoadedResourceCount();
    public IEnumerable<ScheduleResourceDto> GetAllCachedResources();
    public Task RefreshCacheAsync();
    public Task InitializeAsync();
    protected Task LoadScheduleResourceMapping();
    public void RemoveFromMemory(Guid id);
    public Task AddScheduleResourceMap(ScheduleResourceDto map);


}