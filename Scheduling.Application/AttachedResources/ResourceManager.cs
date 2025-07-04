using System.Collections.Concurrent;
using Application.AttachedResources.Service;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.AttachedResources;

[SingletonService]
internal class ResourceManager :IResourceManager
{
     private readonly  IServiceProvider _serviceProvider;
     private readonly  IConfiguration _configuration;
     public  ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
        
        public ResourceManager(IConfiguration configuration,
            IServiceProvider serviceProvider
            )
        {
        
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            InitializeAsync();
        }
        

        public IEnumerable<ScheduleResourceDto> GetResourcesByScheduleId(Guid scheduleId)
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
        

        public IEnumerable<ScheduleResourceDto> GetAllCachedResources()
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
                var _crudService = _serviceProvider.GetRequiredService<ResourceMappingService>(); 
                var allDetails =await  _crudService.GetAllResourceMappingAsync();
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
    
        public  void RemoveFromMemory(Guid id)
        {
            ScheduleResourcesMap.TryRemove(id, out _);
        }

        public async Task AddScheduleResourceMap(ScheduleResourceDto map)
        {  
            var _crudService = _serviceProvider.GetRequiredService<ResourceMappingService>(); 
            await _crudService.AddResourceMappingAsync(map);
            ScheduleResourcesMap.TryAdd(map.ScheduleId, map);
        }
}