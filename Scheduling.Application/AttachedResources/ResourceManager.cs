using Application.AttachedResources.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Serilog;
using System.Collections.Concurrent;

namespace Application.AttachedResources;

internal class ResourceManager : IResourceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    public static ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
    public event EventHandler<List<ScheduleResourceDto>> ScheduleResourcePublish;

    public ResourceManager(IConfiguration configuration,
        IServiceProvider serviceProvider
        )
    {

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        InitializeAsync();
    }


    public List<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId)
    {
        return ScheduleResourcesMap.Values
            .Where(r => r.ScheduleId == scheduleId)
            .ToList();
    }

    public bool IsResourceLoaded(Guid scheduleId)
    {
        return ScheduleResourcesMap.ContainsKey(scheduleId);
    }


    public int GetLoadedResourceCount()
    {
        return ScheduleResourcesMap.Count;
    }


    public List<ScheduleResourceDto> GetAllCachedResources()
    {
        return ScheduleResourcesMap.Values.ToList();
    }

    public async Task RefreshCacheAsync()
    {
        // Clear existing cache
        ScheduleResourcesMap.Clear();

        // Reload from database
        await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        await LoadScheduleResourceMapping();
    }

    public async Task LoadScheduleResourceMapping()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
            var allDetails = await crudService.GetAllResourceMappingAsync();
            foreach (var map in allDetails)
            {
                ScheduleResourcesMap.TryAdd(map.Id, map);
            }
        }
        catch (Exception ex)
        {
            Log.Error("[ScheduleManager][LoadScheduleResourceMapping] : {Message}", ex.Message);
        }
    }

    public void RemoveFromMemory(Guid id)
    {
        ScheduleResourcesMap.TryRemove(id, out _);
    }

    public async Task AddScheduleResourceMap(ScheduleResourceDto map)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
            await crudService.AddResourceMappingAsync(map);
            ScheduleResourcesMap.TryAdd(map.Id, map);
        }
        catch (Exception ex)
        {
            Log.Error("Error in ResourceManager AddScheduleResourceMap ", ex.Message);
        }
    }

    public async Task DeleteScheduleResourceMap(List<Guid> ids, bool Notify = false)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();

            // Collect the deleted ScheduleResourceDto objects
            var deletedResources = new List<ScheduleResourceDto>();

            foreach (var id in ids)
            {
                await crudService.DeleteResourceMappingAsync(id);

                if (ScheduleResourcesMap.TryRemove(id, out var removed))
                {
                    deletedResources.Add(removed);
                }
            }

            // Notify subscribers with the deleted items
            if (Notify && deletedResources.Any())
            {
                ScheduleResourcePublish?.Invoke(this, deletedResources);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in ResourceManager DeleteScheduleResourceMap", ex);
        }
    }


    public List<ScheduleResourceDto> GetScheduleMappingsByResource(Guid resourceId, Resources resourceType)
    {
        return ScheduleResourcesMap.Values
            .Where(x => x.ResourceId == resourceId && x.ResourceType.ToString() == resourceType.ToString())
            .ToList();
    }

}