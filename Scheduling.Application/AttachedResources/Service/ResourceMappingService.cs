using Domain.AttachedResources;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.AttachedResources.DTOs;

namespace Application.AttachedResources.Service;

public class ResourceMappingService :IScheduleResourceService
{
    private readonly IScheduleResourceRepository _resourceRepository;
    private readonly ILogger<ResourceMappingService> _logger;
    
    public ResourceMappingService(
        IScheduleResourceRepository scheduleResourceRepository,ILogger<ResourceMappingService> logger)
    {
        _logger = logger?? throw new ArgumentNullException(nameof(logger));
        _resourceRepository = scheduleResourceRepository;
    }
    
    public  async Task AddResourceMappingAsync(ScheduleResourceDto mapping)
    {
        try
        {
            var entity = mapping.ToDomain();
            await _resourceRepository.AddAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding resource mapping");
            throw;
        }
    }
    public async Task<IEnumerable<ScheduleResourceDto>> GetAllResourceMappingAsync()
    {
        try
        {
            var entities = await _resourceRepository.GetAllAsync();
            return entities.Select(e => e.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource mappings");
            throw;
        }
    }
    public async Task DeleteResourceMappingAsync(Guid mappingId)
    {
        try
        {
            var entity = await _resourceRepository.GetByIdAsync(mappingId);
            if (entity != null)
            {
                await _resourceRepository.DeleteAsync(entity.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resource mapping {MappingId}", mappingId);
            throw;
        }
    }
}