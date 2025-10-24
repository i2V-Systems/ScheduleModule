using Application.AttachedResources.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Serilog;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.AttachedResources;

internal class ResourceManager : IResourceManager
{
    private bool _initialized = false;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid userId;
    public static ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
    public event EventHandler<ScheduleResourceDto> ScheduleResourcePublish;

    public ResourceManager(IConfiguration configuration,
        IServiceProvider serviceProvider,  IHttpContextAccessor httpContextAccessor
        )
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        // InitializeAsync();
        _httpContextAccessor = httpContextAccessor;
        var httpContext = _httpContextAccessor.HttpContext;
        if (
            httpContext != null
            && httpContext.Request.Headers.TryGetValue("Userid", out var userid)
        )
        {
            userId = new Guid(userid);
        }
    }


    public async Task InitializeAsync()
    {
        if (_initialized) return;
        try
        {
            await LoadScheduleResourceMapping();
            _initialized = true;
        }
        catch (Exception ex)
        {
            Log.Error("Exception in initialised schedules");
        }
    }
    
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            await InitializeAsync();
        }
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
        
      
        
        public async Task LoadScheduleResourceMapping()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
                var allDetails =await  crudService.GetAllResourceMappingAsync();
                foreach (var map in allDetails)
                {
                    ScheduleResourcesMap.TryAdd(map.Id, map);
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
                var dto= await crudService.AddResourceMappingAsync(map,userId);
                ScheduleResourcesMap.TryAdd(dto.Id , dto);
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
                ScheduleResourcesMap.TryGetValue(map.Id , out var oldMap);
                var dto= await crudService.UpdateResourceMappingAsync(map,userId);
                ScheduleResourcesMap.TryUpdate(map.Id , dto,oldMap);
            }
            catch (Exception ex)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",ex.Message);
            }
        }
        public async Task<Guid> DeleteScheduleResourceMap(Guid id,bool Notify = false)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
                
                await crudService.DeleteResourceMappingAsync(id,userId);
                
                var mapEntry = ScheduleResourcesMap
                    .FirstOrDefault(m => m.Value.Id == id);
                
                if (mapEntry.Equals(default(KeyValuePair<Guid, ScheduleResourceDto>)))
                {
                    Log.Warning("Resource mapping with Id {Id} not found in memory map.", id);
                    return Guid.Empty;
                }
                
                var mapId = mapEntry.Key;
                if (ScheduleResourcesMap.TryRemove(mapId, out var removedMap))
                {
                    if (Notify && removedMap != null)
                    {
                        ScheduleResourcePublish?.Invoke(this, removedMap);
                    }

                    return mapEntry.Value.ScheduleId;
                }
                else
                {
                    Log.Warning("Failed to remove resource mapping with Id {Id} from memory map.", id);
                    return Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting resource mapping with Id {Id}", id);
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
                    await crudService.DeleteResourceMappingAsync(id, userId);
                    List<Guid>mappingIds=  ScheduleResourcesMap
                        .Where(s => s.Key == id)
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