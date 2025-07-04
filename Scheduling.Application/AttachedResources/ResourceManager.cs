using System.Collections.Concurrent;
using Application.AttachedResources.Service;
using Microsoft.Extensions.Configuration;
using Scheduling.Contracts.AttachedResources.DTOs;
using Serilog;

namespace Application.AttachedResources;

public class ResourceManager
{
     private readonly  IServiceProvider _serviceProvider;
     private readonly  IConfiguration _configuration;
     private readonly ResourceMappingService _crudService;
     public  ConcurrentDictionary<Guid, ScheduleResourceDto> ScheduleResourcesMap { get; } = new();
        
        public ResourceManager(IConfiguration configuration,
            IServiceProvider serviceProvider,
            ResourceMappingService resourceCrudService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _crudService = resourceCrudService;
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
        
        private async Task LoadScheduleResourceMapping()
        {
            try
            {
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
            await _crudService.AddResourceMappingAsync(map);
            ScheduleResourcesMap.TryAdd(map.ScheduleId, map);
        }
}