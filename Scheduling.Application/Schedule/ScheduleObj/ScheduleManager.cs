using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommonUtilityModule.CrudUtilities;
using CommonUtilityModule.Manager;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Serilog;

namespace Application.Schedule.ScheduleObj
{
    internal class ScheduleManager : IScheduleManager
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _initialized = false;
        private readonly IConfiguration _configuration;
        private readonly IResourceManager _resourceManager;
        private readonly IScheduleEventManager _scheduleEventManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private Guid userId;

        public static ConcurrentDictionary<Guid, ScheduleDto> Schedules { get; } = new();
        public static ConcurrentDictionary<Guid, ScheduleAllDetails> ScheduleDetailsMap { get; } = new();

        public ScheduleManager(IConfiguration configuration,
            IServiceProvider serviceProvider,IResourceManager resourceManager,IScheduleEventManager scheduleEventManager,IHttpContextAccessor httpContextAccessor
        )
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _resourceManager = resourceManager;
            _scheduleEventManager = scheduleEventManager;
            _httpContextAccessor = httpContextAccessor;
            var httpContext = _httpContextAccessor.HttpContext;
            if (
                httpContext != null
                && httpContext.Request.Headers.TryGetValue("Userid", out var userid)
            )
            {
                userId = new Guid(userid);
            }
             // InitializeAsync();
        }
        public async Task InitializeAsync()
        {
            if (_initialized) return;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
                var allSchedules = await crudService.GetAllAsync();
                foreach (var schedule in allSchedules)
                {
                    Schedules.TryAdd(schedule.Id, schedule);
                }

                await UpdateScheduleDetails(Schedules.Values);
                _scheduleEventManager.executeLoadedTasks(Schedules);
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

        // Query methods implementation
        public IEnumerable<ScheduleDto> GetSchedulesByIds(IEnumerable<Guid> ids)
        {
            return ids.Where(id => Schedules.ContainsKey(id))
                .Select(id => Schedules[id])
                .ToList();
        }

        public ScheduleDto? GetScheduleFromCache(Guid id)
        {
            return Schedules.TryGetValue(id, out var schedule) ? schedule : null;
        }


        public ScheduleAllDetails? GetScheduleDetailsFromCache(Guid id)
        {
            return ScheduleDetailsMap.TryGetValue(id, out var details) ? details : null;
        }


        // Cache status methods
        public bool IsScheduleLoaded(Guid scheduleId)
        {
            return Schedules.ContainsKey(scheduleId);
        }

        public int GetLoadedScheduleCount()
        {
            return Schedules.Count;
        }

        public IEnumerable<ScheduleDto> GetAllCachedSchedules()
        {
            return Schedules.Values.ToList();
        }

        public async Task RefreshCacheAsync()
        {
            // Clear existing cache
            Schedules.Clear();
            ScheduleDetailsMap.Clear();

            // Reload from database
            await InitializeAsync();
        }

        

        public ScheduleDto Get(Guid id) =>
            Schedules.TryGetValue(id, out var schedule) ? schedule : null;

        public ScheduleAllDetails GetDetailed(Guid id)
        {
            return ScheduleDetailsMap.TryGetValue(id, out var schedule) ? schedule : null;
        }

        public async Task<Guid> CreateScheduleAsync(ScheduleDto schedule, string UserId=null)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            schedule = await crudService.AddAsync(schedule,userId);
            AddToMemory(schedule);
            await _scheduleEventManager.ExecuteAsync(schedule);
            return schedule.Id;
        }

        public async Task UpdateScheduleAsync(ScheduleDto schedule)
        {    
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            await crudService.UpdateAsync(schedule,userId);
            UpdateInMemory(schedule);
            await  _scheduleEventManager.UpdateAsync(schedule);
        }

        public async Task<bool> DeleteScheduleAsync(Guid id)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            await crudService.DeleteAsync(id,userId);
            RemoveFromMemory(id);
            await _scheduleEventManager.DeleteAsync(id);
            return true;
        }

        public async Task<IEnumerable<ScheduleAllDetails>> GetScheduleWithAllDetails(
            string userName
        )
        {
            try
            {
                if (ScheduleDetailsMap.IsEmpty)
                {
                   await UpdateScheduleDetails(Schedules.Values);
                }

                return userName == "admin"
                    ? ScheduleDetailsMap.Values
                    : ImmutableList<ScheduleAllDetails>.Empty;
            }
            catch (Exception ex)
            {
                Log.Error("[ScheduleManager][GetScheduleWithAllDetails] : {Message}", ex.Message);
                return null;
            }
        }

        public IEnumerable<ScheduleDto> GetAllSchedules()
        {
            try
            {
                if (ScheduleDetailsMap.IsEmpty)
                {
                    UpdateScheduleDetails(Schedules.Values);
                }

                return Schedules.Values;
            }
            catch (Exception ex)
            {
                Log.Error("[ScheduleManager][GetScheduleWithAllDetails] : {Message}", ex.Message);
                return null;
            }
        }

        public async Task UpdateMultipleSchedulesAsync(IEnumerable<ScheduleAllDetails> schedules)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            
            foreach (var schedule in schedules)
            {
               await crudService.UpdateAsync(schedule.schedules,userId);
               UpdateInMemory(schedule.schedules);
               await  _scheduleEventManager.UpdateAsync(schedule.schedules);
                
            }
        }
        public async Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                if (Schedules.TryGetValue(id, out var schedule))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
                    await crudService.DeleteAsync(id,userId);
                    RemoveFromMemory(id);
                    await _scheduleEventManager.DeleteAsync(id);
                }
            }
        }

        public async Task SendCrudDataToClientAsync(CrudMethodType method, Dictionary<string, dynamic> resources,
            List<string> skipUserIds = null,
            List<string> targetUserIds = null)
        {
            await CrudManager.SendCrudDataToClient(
                CrudRelatedEntity.Schedule,
                method,
                resources,
                skipUserIds,
                targetUserIds
            );
        }


        private async Task UpdateScheduleDetails(IEnumerable<ScheduleDto> schedules)
        {
            foreach (var schedule in schedules)
            {
                var resource = _resourceManager.GetResourcesByScheduleId(schedule.Id);
                var details = new ScheduleAllDetails
                {
                    schedules = schedule,
                    AttachedResources = resource
                };
                AddOrUpdateScheduleDetails(details);
            }
        }

        public void AddOrUpdateScheduleDetails(ScheduleAllDetails details)
        {
            try
            {
                ScheduleDetailsMap[details.schedules.Id] = details;
            }
            catch (Exception ex)
            {
                Log.Error("[ScheduleManager][AddOrUpdateScheduleDetails] : {Message}", ex.Message);
            }
        }

        public bool IsScheduleNameAvailableAsync(string name,Guid? id=null)
        {
            try
            {
                KeyValuePair<Guid,ScheduleDto> existingSchedule =  Schedules
                    .FirstOrDefault(s => s.Value.Name.ToLower() == name.ToLower() &&  (id == null || s.Value.Id != id));
        
                return existingSchedule.Value==null;
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking schedule name in database: {ex.Message}", ex);
                throw;
            }
        }

        //memory functions 
        public void UpdateInMemory(ScheduleDto schedule)
        {
            if (Schedules.TryGetValue(schedule.Id, out var existing))
            {
                Schedules.TryUpdate(schedule.Id, schedule, existing);
                List<ScheduleResourceDto> resourceDtos=  _resourceManager.GetResourcesByScheduleId(schedule.Id);
                var updatedDetails = new ScheduleAllDetails
                {
                    schedules = schedule,
                    AttachedResources = resourceDtos ?? null
                };
                AddOrUpdateScheduleDetails(updatedDetails);
                
            }
        }

        public void RemoveFromMemory(Guid id)
        {
            Schedules.TryRemove(id, out _);
            _resourceManager.RemoveFromMemorywithScheduleId(id);
            ScheduleDetailsMap.TryRemove(id, out _);
        }


        public void AddToMemory(ScheduleDto schedule)
        {
            Schedules.TryAdd(schedule.Id, schedule);
            AddOrUpdateScheduleDetails(new ScheduleAllDetails { schedules = schedule });
        }
      
    }
}