using CommonUtilityModule.CrudUtilities;
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.Schedule;

public interface IScheduleManager
{
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
        Task<Guid> CreateScheduleAsync(ScheduleDto dto, string userId = null);
        Task UpdateScheduleAsync(ScheduleDto dto);
        Task<bool> DeleteScheduleAsync(Guid id);

        // Complex queries
        Task<IEnumerable<ScheduleAllDetails>> GetScheduleWithAllDetails(string userName);
        IEnumerable<ScheduleDto> GetAllSchedules();

        // Memory management operations
        void AddToMemory(ScheduleDto schedule);
        void UpdateInMemory(ScheduleDto schedule);
        void RemoveFromMemory(Guid id);
        void AddOrUpdateScheduleDetails(ScheduleAllDetails details);
        bool IsScheduleNameAvailable(string name,Guid? id=null);
        Task<List<ScheduleAllDetails>> CreateAndUpdateResourceMapping(ScheduleResourceDto resourceMap);

        // Bulk operations
        Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids);
        Task UpdateMultipleSchedulesAsync(IEnumerable<ScheduleAllDetails> schedules);
        Dictionary<string, dynamic> GetAllDetailNotificationObj(List<ScheduleAllDetails> updatedSchedule);

        // Cross-cutting concerns

}
