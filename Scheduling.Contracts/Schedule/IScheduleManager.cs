using System.Collections.Concurrent;
using CommonUtilityModule.CrudUtilities;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.Schedule;

public interface IScheduleManager
{
        // In-memory cache, formerly exposed as public static fields on the ScheduleManager
        // class; now instance state on the singleton-registered IScheduleManager.
        ConcurrentDictionary<Guid, ScheduleDto> Schedules { get; }
        ConcurrentDictionary<Guid, ScheduleAllDetails> ScheduleDetailsMap { get; }

        // Initialization and lifecycle
         Task InitializeAsync();

        // Query methods for dictionary access
        IEnumerable<ScheduleDto> GetSchedulesByIds(IEnumerable<Guid> ids);
        ScheduleDto? GetScheduleFromCache(Guid id);
        ScheduleAllDetails? GetScheduleDetailsFromCache(Guid id);

        // Cache status methods
        bool IsScheduleLoaded(Guid scheduleId);
        int GetLoadedScheduleCount();

        // Bulk cache operations
        IEnumerable<ScheduleDto> GetAllCachedSchedules();
        Task RefreshCacheAsync();

        // Core CRUD operations
        ScheduleDto Get(Guid id);
        ScheduleAllDetails GetDetailed(Guid id);
        Task<ScheduleAllDetails> CreateScheduleAsync(ScheduleDto dto);
        Task<ScheduleAllDetails> UpdateScheduleAsync(ScheduleDto dto);
        Task DeleteScheduleAsync(Guid id);
        Task SendClientNotificationWithSchedule(List<ScheduleAllDetails> scheduleAllDetails,
          CrudMethodType methodType);

        // Complex queries
        Task<IEnumerable<ScheduleAllDetails>> GetScheduleWithAllDetails();
        IEnumerable<ScheduleDto> GetAllSchedules();

        // Memory management operations
        void AddToMemory(ScheduleDto schedule);
        Task<ScheduleAllDetails> UpdateInMemory(ScheduleDto schedule );
        void RemoveFromMemory(Guid id);
        void AddOrUpdateScheduleDetails(ScheduleAllDetails details);
        bool IsScheduleNameAvailable(string name,Guid? id=null);
        Task<List<ScheduleAllDetails>> CreateAndUpdateResourceMapping(ScheduleResourceDto resourceMap);

        // Bulk operations
        Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids);
        Task UpdateMultipleSchedulesAsync(List<ScheduleAllDetails> schedules);
        Dictionary<string, dynamic> GetAllDetailNotificationObj(List<ScheduleAllDetails> updatedSchedule);
        Task<ScheduleAllDetails> CreateResourceMapping(ScheduleResourceDto resourceMap);

        Task DeleteAttachedResources(List<DetachScheduleResourceDto> resourceDto);

        // Cross-cutting concerns

}
