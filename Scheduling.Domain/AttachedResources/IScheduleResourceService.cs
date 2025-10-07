using Scheduling.Contracts.AttachedResources.DTOs;

namespace Domain.AttachedResources;

public interface  IScheduleResourceService
{
    // Resource mapping methods
    Task<ScheduleResourceDto> AddResourceMappingAsync(ScheduleResourceDto mapping, Guid userId);
    Task<IEnumerable<ScheduleResourceDto>> GetAllResourceMappingAsync();
    Task DeleteResourceMappingAsync(Guid mappingId,Guid userId);
}