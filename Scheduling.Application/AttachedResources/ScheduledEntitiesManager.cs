using Application.AttachedResources.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.AttachedResources;

internal class ScheduledEntitiesManager : IScheduledEntitiesManager
{
    private bool _initialized = false;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private Guid userId;
    public static ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
    public event EventHandler<ScheduleResourceDto> ScheduleResourcePublish;

    public ScheduledEntitiesManager(IConfiguration configuration,
        IServiceProvider serviceProvider,  IHttpContextAccessor httpContextAccessor
        )
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        // InitializeAsync();

        IHttpContextAccessor    _httpContextAccessor = httpContextAccessor;
        var httpContext = _httpContextAccessor.HttpContext;
        if (
            httpContext != null
            && httpContext.Request.Headers.TryGetValue("Userid", out StringValues userid)
        )
        {

            userId = new Guid(userid.ToString() );
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
        catch (Exception exception)
        {
            Log.Error("Exception in initialised schedules :" + exception.Message, exception);
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
             .Where(scheduleResourceDto => scheduleResourceDto.ScheduleId == scheduleId)
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
        _initialized = false;
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
            catch(Exception exception)
            {
                Log.Error("[ScheduleManager][LoadScheduleResourceMapping] : {Message}", exception.Message);
            }
        }

        public void RemoveFromMemorywithScheduleId(Guid scheduleId)
        {
            var mappingIds=ScheduleResourcesMap
                .Where(keyValuePair=>keyValuePair.Value.ScheduleId==scheduleId)
                .Select(keyValuePair=>keyValuePair.Key)
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
            catch (Exception exception)
            {

                Log.Error("Error in ResourceManager AddScheduleResourceMap ",exception.Message);
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
                if (oldMap is not null)
                {
                  ScheduleResourcesMap.TryUpdate(map.Id , dto,oldMap);

                }
                else
                {
                  throw new  NullReferenceException("oldMap is null" );
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",exception.Message);
            }
        }
        public async Task<Guid> DeleteScheduleResourceMap(Guid id, bool notify = false)
        {
          try
          {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();

            await crudService.DeleteResourceMappingAsync(id, userId);

            if (!TryRemoveFromMemory(id, out var removedMap, out var scheduleId))
            {
              Log.Warning("Resource mapping with Id {Id} not found or failed removal.", id);
              return Guid.Empty;
            }

            if (notify)
              ScheduleResourcePublish?.Invoke(this, removedMap);

            await HandleDetachAsync(scope.ServiceProvider, removedMap);

            return scheduleId;
          }
          catch (Exception exception)
          {
            Log.Error(exception, "Error deleting resource mapping with Id {Id}", id);
            return Guid.Empty;
          }
        }
        private async Task HandleDetachAsync(IServiceProvider provider, ScheduleResourceDto removedMap)
        {
          var detachHandlers = provider.GetServices<IResourceDetachHandler>();

          foreach (var handler in detachHandlers
                     .Where(handler => handler.ResourceType == removedMap.ResourceType))
          {
            await handler.OnDetachedAsync(removedMap);
          }
        }
        private bool TryRemoveFromMemory(Guid id, [NotNullWhen(true)]out ScheduleResourceDto? removedMap, out Guid scheduleId)
        {
          removedMap = null;
          scheduleId = Guid.Empty;

          var mapEntry = ScheduleResourcesMap
            .FirstOrDefault(kvp => kvp.Value.Id == id);

          if (mapEntry.Equals(default(KeyValuePair<Guid, ScheduleResourceDto>)))
            return false;

          if (!ScheduleResourcesMap.TryRemove(mapEntry.Key, out removedMap))
            return false;

          scheduleId = mapEntry.Value.ScheduleId;
          return true;
        }

        public async Task DeleteMultipleResources(List<DetachScheduleResourceDto> resources)
        {
          try
          {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ResourceMappingService>();
            foreach (var mapping in resources)
            {
              Guid mappingId = await crudService.DeleteResourceSchdeuleMappingAsync(mapping,userId);
              ScheduleResourcesMap.TryRemove(mappingId, out var map);
            }

          }
          catch (Exception exception)
          {
            Log.Error("Error in ResourceManager DeleteMultipleResources : ",exception.Message);

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
                        .Where(keyValuePair => keyValuePair.Key == id)
                        .Select(keyValuePair => keyValuePair.Key).ToList();
                    foreach (var mapId in mappingIds)
                    {
                        ScheduleResourcesMap.TryRemove(mapId, out var map);
                    }

                }
            }
            catch (Exception exception)
            {
                Log.Error("Error in ResourceManager AddScheduleResourceMap ",exception.Message);

            }
            return scheduleAllDetails;
        }
}
