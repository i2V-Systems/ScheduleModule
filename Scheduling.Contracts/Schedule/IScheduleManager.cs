using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.Schedule;

public interface IScheduleManager
{
    Task InitializeAsync();

    ScheduleDto? GetScheduleFromCache(Guid id);
    ScheduleAllDetails? GetScheduleDetailsFromCache(Guid id);

    bool IsScheduleLoaded(Guid scheduleId);
    IEnumerable<ScheduleDto> GetAllCachedSchedules();

    ScheduleDto Get(Guid id);
    Task<ScheduleAllDetails> CreateScheduleAsync(ScheduleDto dto, string userId = null);
    Task<ScheduleAllDetails> UpdateScheduleAsync(ScheduleDto dto);
    Task DeleteScheduleAsync(Guid id);

    Task<IEnumerable<ScheduleAllDetails>> GetScheduleWithAllDetails(string userName);

    Task<ScheduleAllDetails> UpdateInMemory(ScheduleDto schedule);
    bool IsScheduleNameAvailable(string name, Guid? id = null);
    Task<List<ScheduleAllDetails>> CreateAndUpdateResourceMapping(ScheduleResourceDto resourceMap);

    Task DeleteMultipleSchedulesAsync(IEnumerable<Guid> ids);
    Task UpdateMultipleSchedulesAsync(List<ScheduleAllDetails> schedules);
}
