using Scheduling.Contracts.AttachedResources.DTOs;

namespace Domain.AttachedResources;

public interface  IScheduleResourceService
{
    // Resource mapping methods
    Task<ScheduleResourceDto> AddResourceMappingAsync(ScheduleResourceDto mapping);
    Task<ScheduleResourceDto> UpdateResourceMappingAsync(ScheduleResourceDto mapping);
    Task<IEnumerable<ScheduleResourceDto>> GetAllResourceMappingAsync();
    Task DeleteResourceMappingAsync(Guid mappingId);
    Task<Guid> DeleteResourceSchdeuleMappingAsync(DetachScheduleResourceDto mapping);
    Task<string>GetAttachedResourceStringsAsync(Guid scheduleId);
}
