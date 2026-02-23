using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommonUtilityModule.CrudUtilities;
using CommonUtilityModule.Manager;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts;
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
        private readonly IScheduledEntitiesManager _scheduledEntitiesManager;
        private readonly IScheduleEventManager _scheduleEventManager;
        private readonly INotificationManager _notificationManager;
        private Guid userId;

        public static ConcurrentDictionary<Guid, ScheduleDto> Schedules { get; } = new();
        public static ConcurrentDictionary<Guid, ScheduleAllDetails> ScheduleDetailsMap { get; } = new();

        public ScheduleManager(IConfiguration configuration,
            IServiceProvider serviceProvider,
            IScheduledEntitiesManager scheduledEntitiesManager,
            IScheduleEventManager scheduleEventManager,
            INotificationManager notificationManager,
            IHttpContextAccessor httpContextAccessor
        )
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _scheduledEntitiesManager = scheduledEntitiesManager;
            _scheduleEventManager = scheduleEventManager;
            _notificationManager = notificationManager;
            IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
            var httpContext = _httpContextAccessor.HttpContext;
            if (
                httpContext != null
                && httpContext.Request.Headers.TryGetValue("Userid", out var userid)
            )
            {
                userId = new Guid(userid.ToString());
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

                 UpdateScheduleDetails(Schedules.Values);
                _scheduleEventManager.executeLoadedTasks(Schedules);
                _initialized = true;
            }
            catch (Exception exception)
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
            return Schedules.TryGetValue(id, out var schedule) ? schedule : throw new NullReferenceException("Schedule not found : GetScheduleFromCache");
        }


        public ScheduleAllDetails GetScheduleDetailsFromCache(Guid id)
        {
          if (ScheduleDetailsMap.TryGetValue(id, out var details))
            return details;

          throw new KeyNotFoundException($"Schedule details not found for Id: {id}");
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
            _initialized = false;
            await InitializeAsync();
        }



        public ScheduleDto Get(Guid id) =>
            Schedules.TryGetValue(id, out var schedule) ? schedule : throw new NullReferenceException("Schedule not found ScheduleManager Get()");

        public ScheduleAllDetails GetDetailed(Guid id)
        {
            return ScheduleDetailsMap.TryGetValue(id, out var schedule) ? schedule : throw new NullReferenceException("Schedule not found ScheduleManager GetDetailed()");
        }

        public async Task<ScheduleAllDetails> CreateScheduleAsync(ScheduleDto scheduleDto, string? UserId=null)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            scheduleDto = await crudService.AddAsync(scheduleDto,userId);
            AddToMemory(scheduleDto);
            await _scheduleEventManager.ExecuteAsync(scheduleDto);
            ScheduleAllDetails scheduleAllDetails =  GetDetailed(scheduleDto.Id) ?? throw new InvalidOperationException();
            List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>() {scheduleAllDetails};
            await SendClientNotificationWithSchedule(scheduleAllDetailsList ,CrudMethodType.Add);

            return scheduleAllDetails;
        }

        public async Task SendClientNotificationWithSchedule(List<ScheduleAllDetails> scheduleAllDetails,CrudMethodType methodType)
        {
          var objectToSend = GetAllDetailNotificationObj(scheduleAllDetails );
          await _notificationManager.SendCrudDataToClientAsync(
            methodType,
            objectToSend
          );
        }
        public async Task<ScheduleAllDetails> UpdateScheduleAsync(ScheduleDto scheduleDto)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
            await crudService.UpdateAsync(scheduleDto,userId);
            UpdateInMemory(scheduleDto);
            await  _scheduleEventManager.UpdateAsync(scheduleDto);
            ScheduleAllDetails scheduleAllDetails =  GetDetailed(scheduleDto.Id)?? throw new InvalidOperationException();
            List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>() {scheduleAllDetails};
            await SendClientNotificationWithSchedule( scheduleAllDetailsList,CrudMethodType.Update);
            return scheduleAllDetails;
        }

        public async Task DeleteScheduleAsync(Guid id)
        {
          using var scope = _serviceProvider.CreateScope();
          var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();
          ScheduleAllDetails? scheduleAllDetails = GetScheduleDetailsFromCache(id);
          await crudService.DeleteAsync(id, userId);
          RemoveFromMemory(id);
          await _scheduleEventManager.DeleteAsync(id);
          if (scheduleAllDetails != null)
          {
            List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>() {scheduleAllDetails};

            await SendClientNotificationWithSchedule(scheduleAllDetailsList, CrudMethodType.Delete);
          }
        }

        public async Task<IEnumerable<ScheduleAllDetails>> GetScheduleWithAllDetails()
        {
            try
            {
                if (ScheduleDetailsMap.IsEmpty)
                {
                    UpdateScheduleDetails(Schedules.Values);
                }

                return ScheduleDetailsMap.Values;
            }
            catch (Exception exception)
            {
                Log.Error("[ScheduleManager][GetScheduleWithAllDetails] : {Message}", exception.Message);
                throw;
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
            catch (Exception exception)
            {
                Log.Error("[ScheduleManager][GetScheduleWithAllDetails] : {Message}", exception.Message);
                throw;
            }
        }

        public async Task UpdateMultipleSchedulesAsync(List<ScheduleAllDetails> schedules)
        {
            using var scope = _serviceProvider.CreateScope();
            var crudService = scope.ServiceProvider.GetRequiredService<ScheduleCrudService>();

            foreach (var schedule in schedules)
            {
               await crudService.UpdateAsync(schedule.schedules,userId);
               UpdateInMemory(schedule.schedules);
               await  _scheduleEventManager.UpdateAsync(schedule.schedules);

            }

            await SendClientNotificationWithSchedule(schedules, CrudMethodType.Update);
        }
        public async Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids)
        {
            List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>();
            foreach (var id in ids)
            {
              scheduleAllDetailsList.Add(
                GetScheduleDetailsFromCache(id)?? GetDetailed(id)
              );
            }

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

            await SendClientNotificationWithSchedule(scheduleAllDetailsList, CrudMethodType.Delete);
        }




        private void UpdateScheduleDetails(IEnumerable<ScheduleDto> schedules)
        {
            foreach (var schedule in schedules)
            {
                var resource = _scheduledEntitiesManager.GetResourcesByScheduleId(schedule.Id);
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
            catch (Exception exception)
            {
                Log.Error("[ScheduleManager][AddOrUpdateScheduleDetails] : {Message}", exception.Message);
            }
        }

        public bool IsScheduleNameAvailable(string name,Guid? id=null)
        {
            try
            {
                KeyValuePair<Guid,ScheduleDto> existingSchedule =  Schedules
                    .FirstOrDefault(keyValuePair => keyValuePair.Value.Name.ToLower() == name.ToLower() &&  (id == null || keyValuePair.Value.Id != id));

                return existingSchedule.Value==null;
            }
            catch (Exception exception)
            {
                Log.Error($"Error checking schedule name in database: {exception.Message}", exception);
                throw;
            }
        }

        //memory functions
        public async Task<ScheduleAllDetails>  UpdateInMemory(ScheduleDto schedule)
        {
          try
          {
            if (Schedules.TryGetValue(schedule.Id, out ScheduleDto? existing))
            {
              Schedules.TryUpdate(schedule.Id, schedule, existing);
              List<ScheduleResourceDto> resourceDtos = _scheduledEntitiesManager.GetResourcesByScheduleId(schedule.Id);
              var updatedDetails = new ScheduleAllDetails
              {
                schedules = schedule,
                AttachedResources = resourceDtos ?? null
              };
              AddOrUpdateScheduleDetails(updatedDetails);
              ScheduleAllDetails updatedSchedule = GetScheduleDetailsFromCache(schedule.Id);
              List<ScheduleAllDetails> scheduleAllDetailsList = new List<ScheduleAllDetails>() { updatedSchedule };
              await SendClientNotificationWithSchedule(scheduleAllDetailsList,
                  CrudMethodType.Update);

              return updatedSchedule;
            }
            else
            {
              throw new KeyNotFoundException(
                $"Schedule not found in memory [UpdateInMemory]. Id={schedule.Id}");
            }
          }
          catch (Exception exception)
          {
            Log.Error("Exception in [UpdateInMemory]",exception.Message);
            throw;
          }

        }

        public void RemoveFromMemory(Guid id)
        {
            Schedules.TryRemove(id, out _);
            _scheduledEntitiesManager.RemoveFromMemorywithScheduleId(id);
            ScheduleDetailsMap.TryRemove(id, out _);
        }


        public void AddToMemory(ScheduleDto schedule)
        {
            Schedules.TryAdd(schedule.Id, schedule);
            ScheduleAllDetails scheduleAllDetails = new ScheduleAllDetails { schedules = schedule };
            AddOrUpdateScheduleDetails(scheduleAllDetails);
        }

        private List<ScheduleResourceDto> GetExistingMappings(
          ScheduleResourceDto resourceMap)
        {
          var allResources = _scheduledEntitiesManager.GetAllCachedResources();

          return allResources
            .Where(scheduleResourceDto =>
              scheduleResourceDto.ResourceId == resourceMap.ResourceId &&
              scheduleResourceDto.ResourceType == resourceMap.ResourceType)
            .ToList();
        }
        private async Task RemoveExistingMappings(
          IEnumerable<ScheduleResourceDto> existingMappings,
          List<ScheduleAllDetails> affectedSchedules)
        {
          foreach (var map in existingMappings)
          {
            await _scheduledEntitiesManager.DeleteScheduleResourceMap(map.Id);
            AddScheduleToAffectedList(map.ScheduleId, affectedSchedules);
          }
        }
        private async Task AddNewMappingIfNeeded(
          ScheduleResourceDto resourceMap,
          List<ScheduleAllDetails> affectedSchedules)
        {
          if (resourceMap.ScheduleId == Guid.Empty)
            return;

          await _scheduledEntitiesManager.AddScheduleResourceMap(resourceMap);

          AddScheduleToAffectedList(
            resourceMap.ScheduleId,
            affectedSchedules,
            removeDuplicates: true);
        }
        private async Task AddScheduleToAffectedList(
          Guid scheduleId,
          List<ScheduleAllDetails> affectedSchedules,
          bool removeDuplicates = false)
        {
          var schedule = GetScheduleFromCache(scheduleId);
          if (schedule == null)
            return;

        var _ =  await  UpdateInMemory(schedule);

          var scheduleDetails = GetScheduleDetailsFromCache(scheduleId);
          if (scheduleDetails == null)
            return;

          if (removeDuplicates)
          {
            affectedSchedules.RemoveAll(
              scheduleAllDetails => scheduleAllDetails.schedules.Id == scheduleDetails.schedules.Id);
          }

          affectedSchedules.Add(scheduleDetails);
        }

        public async Task<List<ScheduleAllDetails>> CreateAndUpdateResourceMapping(
          ScheduleResourceDto resourceMap)
        {
          var affectedSchedules = new List<ScheduleAllDetails>();

          var existingMappings = GetExistingMappings(resourceMap);
          await RemoveExistingMappings(existingMappings, affectedSchedules);

          await AddNewMappingIfNeeded(resourceMap, affectedSchedules);

          await SendClientNotificationWithSchedule(
            affectedSchedules,
            CrudMethodType.Update);

          return affectedSchedules;
        }

        public Dictionary<string, dynamic> GetAllDetailNotificationObj(List<ScheduleAllDetails> updatedSchedule)
        {
          var objectToSend =
            new Dictionary<string, dynamic>()
            {
              {
                "scheduleAllDetailsList",
                updatedSchedule
              },
            };
          return objectToSend;
        }

        public async Task<ScheduleAllDetails> CreateResourceMapping(ScheduleResourceDto resourceDto)
        {
          await _scheduledEntitiesManager.AddScheduleResourceMap(resourceDto);
          var schedule = GetDetailed(resourceDto.ScheduleId);
          ScheduleAllDetails scheduleAllDetails = await UpdateInMemory(schedule.schedules);
          return scheduleAllDetails;
        }

        public async Task DeleteAttachedResources(List<DetachScheduleResourceDto> resourceDto)
        {
          await _scheduledEntitiesManager.DeleteMultipleResources(resourceDto);
          await _scheduledEntitiesManager.RefreshCacheAsync();
          await RefreshCacheAsync();
          IEnumerable<ScheduleAllDetails> updatedSchedule  = await GetScheduleWithAllDetails();
          await SendClientNotificationWithSchedule( updatedSchedule.ToList() , CrudMethodType.Update);
        }
    }
}
