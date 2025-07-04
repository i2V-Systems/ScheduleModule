using System.Collections.Concurrent;
using System.Collections.Immutable;
using Application.AttachedResources;
using Application.Schedule.ScheduleEvent;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Application.Schedule.ScheduleEvent.Scheduler;
using CommonUtilityModule.CrudUtilities;
using CommonUtilityModule.Manager;
using Coravel.Events.Interfaces;
using Domain.Schedule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.DTOs;
using Serilog;

namespace Application.Schedule.ScheduleObj
{
    internal class ScheduleManager : IScheduleManager
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IUnifiedScheduler _scheduler;
        private readonly IConfiguration _configuration;
        private readonly ResourceManager _resourceManager;

        public ConcurrentDictionary<Guid, ScheduleDto> Schedules { get; } = new();
        public ConcurrentDictionary<Guid, ScheduleAllDetails> ScheduleDetailsMap { get; } = new();

        public   ScheduleManager(IConfiguration configuration,
            IUnifiedScheduler scheduler,
            IDispatcher dispatcher,
            IServiceProvider serviceProvider
        )
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _resourceManager = serviceProvider.GetRequiredService<ResourceManager>();
            

             InitializeAsync();
        }

        // Query methods implementation
        public IEnumerable<ScheduleDto> GetSchedulesByIds(IEnumerable<Guid> ids)
        {
            return ids.Where(id => Schedules.ContainsKey(id))
                .Select(id => Schedules[id])
                .ToList();
        }

        public ScheduleDto GetScheduleFromCache(Guid id)
        {
            return Schedules.TryGetValue(id, out var schedule) ? schedule : null;
        }


        public ScheduleAllDetails GetScheduleDetailsFromCache(Guid id)
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

        public async Task InitializeAsync()
        {
            var _crudService = _serviceProvider.GetRequiredService<ScheduleCrudService>(); 
            ScheduleEventManager.Init(_serviceProvider, _configuration); //TODO 
            var allSchedules = await _crudService.GetAllAsync();
            foreach (var schedule in allSchedules)
            {
                Schedules.TryAdd(schedule.Id, schedule);
            }

            UpdateScheduleDetails(Schedules.Values);
            ScheduleEventManager.executeLoadedTasks(Schedules,_scheduler);
        }

        public ScheduleDto Get(Guid id) =>
            Schedules.TryGetValue(id, out var schedule) ? schedule : null;

        public ScheduleAllDetails GetDetailed(Guid id)
        {
            return ScheduleDetailsMap.TryGetValue(id, out var schedule) ? schedule : null;
        }

        public async Task<Guid> CreateScheduleAsync(ScheduleDto schedule, string userId = null)
        {
            var _crudService = _serviceProvider.GetRequiredService<ScheduleCrudService>(); 
            var id = await _crudService.AddAsync(schedule);
            AddToMemory(id, schedule);
            List<ScheduleResourceDto> resources = _resourceManager.GetResourcesByScheduleId(id).ToList();
            await ScheduleEventManager.ExecuteAsync(schedule, _scheduler, resources);
            return id;
        }

        public async Task UpdateScheduleAsync(ScheduleDto schedule)
        {    
            var _crudService = _serviceProvider.GetRequiredService<ScheduleCrudService>(); 
            await _crudService.UpdateAsync(schedule);
            UpdateInMemory(schedule);
            await  ScheduleEventManager.UpdateAsync(schedule);
        }

        public async Task<bool> DeleteScheduleAsync(Guid id)
        {
            var _crudService = _serviceProvider.GetRequiredService<ScheduleCrudService>(); 
            await _crudService.DeleteAsync(id);
            RemoveFromMemory(id);
            await ScheduleEventManager.DeleteAsync(id);
            ScheduleEventManager.scheduleEventService.UnscheduleJob(id, _scheduler);
            return true;
        }

        public IEnumerable<ScheduleAllDetails> GetScheduleWithAllDetails(
            string userName
        )
        {
            try
            {
                if (ScheduleDetailsMap.IsEmpty)
                {
                    UpdateScheduleDetails(Schedules.Values);
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

        public async Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids)
        {
            foreach (var id in ids)
            {
                if (Schedules.TryGetValue(id, out var schedule))
                {
                    var _crudService = _serviceProvider.GetRequiredService<ScheduleCrudService>(); 
                    await _crudService.DeleteAsync(id);
                    RemoveFromMemory(id);
                    await ScheduleEventManager.DeleteAsync(id);
                    ScheduleEventManager.scheduleEventService.UnscheduleJob(id, _scheduler);
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


        private void UpdateScheduleDetails(IEnumerable<ScheduleDto> schedules)
        {
            foreach (var schedule in schedules)
            {
                var details = new ScheduleAllDetails { schedules = schedule };
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


        //memory functions 
        public void UpdateInMemory(ScheduleDto schedule)
        {
            if (Schedules.TryGetValue(schedule.Id, out var existing))
            {
                Schedules.TryUpdate(schedule.Id, schedule, existing);
                var updatedDetails = new ScheduleAllDetails
                {
                    schedules = schedule,
                    AttachedResources = ScheduleDetailsMap.ContainsKey(schedule.Id)
                        ? ScheduleDetailsMap[schedule.Id].AttachedResources
                        : null
                };
                AddOrUpdateScheduleDetails(updatedDetails);
            }
        }

        public void RemoveFromMemory(Guid id)
        {
            Schedules.TryRemove(id, out _);
            _resourceManager.RemoveFromMemory(id);
            ScheduleDetailsMap.TryRemove(id, out _);
        }


        public void AddToMemory(Guid id, ScheduleDto schedule)
        {
            Schedules.TryAdd(id, schedule);
            AddOrUpdateScheduleDetails(new ScheduleAllDetails { schedules = schedule });
        }

        //event fun
      
    }
}