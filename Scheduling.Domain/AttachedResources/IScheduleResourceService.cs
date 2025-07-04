using Scheduling.Contracts.AttachedResources.DTOs;

namespace Domain.AttachedResources;

public interface  IScheduleResourceService
{
    // Resource mapping methods
    Task AddResourceMappingAsync(ScheduleResourceDto mapping);
    Task<IEnumerable<ScheduleResourceDto>> GetAllResourceMappingAsync();
    Task DeleteResourceMappingAsync(Guid mappingId);
}