using Application.AttachedResources.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Serilog;
using System.Collections.Concurrent;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.AttachedResources;

internal class ResourceManager : IResourceManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    public static ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
    public event EventHandler<ScheduleResourceDto> ScheduleResourcePublish;

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
        
        public bool IsResourceLoaded(Guid mappingId)
        {
            return ScheduleResourcesMap.ContainsKey(mappingId);
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
                var allDetails =await  crudService.GetAllResourceMappingAsync();
                foreach (var map in allDetails)
                {
                    ScheduleResourcesMap.TryAdd(map.MapId, map);
                }
            }
            catch(Exception ex)
            {
                Log.Error("[ScheduleManager][LoadScheduleResourceMapping] : {Message}", ex.Message);
            }
        }

        public void RemoveFromMemorywithScheduleId(Guid scheduleId)
        {
            var mappingIds=ScheduleResourcesMap
                .Where(s=>s.Value.ScheduleId==scheduleId)
                .Select(s=>s.Key)
                .ToList();
            foreach (var mapId in mappingIds)
            {
                ScheduleResourcesMap.TryRemove(mapId, out _);
            }
        }
        public  void RemoveFromMemory(Guid mapId)
        {
            ScheduleResourcesMap.TryRemove(mapId, out _);
        }

        public async Task AddScheduleResourceMap(ScheduleResourceDto map)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
                await crudService.AddResourceMappingAsync(map);
                ScheduleResourcesMap.TryAdd(map.MapId, map);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",ex.Message);
            }
        }

        public async Task UpdateScheduleResourceMap(ScheduleResourceDto map)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
                ScheduleResourcesMap.TryGetValue(map.MapId, out var oldMap);
                await crudService.UpdateResourceMappingAsync(map);
                ScheduleResourcesMap.TryUpdate(map.MapId, map,oldMap);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",ex.Message);
            }
        }
        public async Task<Guid> DeleteScheduleResourceMap(Guid id, bool Notify = false)
        {
            Guid mapId = Guid.Empty;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();

                await crudService.DeleteResourceMappingAsync(id);
                mapId= ScheduleResourcesMap
                    .Where(map => map.Value.ResourceId == id)
                    .Select(s => s.Key)
                    .FirstOrDefault();
                ScheduleResourcesMap.TryRemove(mapId, out var map);
                if (Notify)
                {
                    ScheduleResourcePublish?.Invoke(this, map);
                }
                return mapId;
                

            }
            catch (Exception ex)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ", ex.Message);
                return Guid.Empty;
            }

          

        }

        public async Task<ScheduleAllDetails> DeleteMultipleScheduleResourceMap(List<Guid> ids,ScheduleAllDetails scheduleAllDetails)
        {
           
            try
            {
                using var scope = _serviceProvider.CreateScope(); 
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
                foreach (var id in ids)
                {
                    await crudService.DeleteResourceMappingAsync(id);
                    List<Guid>mappingIds=  ScheduleResourcesMap
                        .Where(s => s.Value.ScheduleId == scheduleAllDetails.schedules.Id)
                        .Select(t => t.Key).ToList();
                    foreach (var mapId in mappingIds)
                    {
                        ScheduleResourcesMap.TryRemove(mapId, out var map);
                    }
                   
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",ex.Message);
                
            }
            return scheduleAllDetails;
        }
}