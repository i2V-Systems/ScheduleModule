using Application.Schedule;
using AutoMapper;
using Domain.AttachedResources;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.AttachedResources.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.AttachedResources.Service;

[TransientService]
public class ResourceMappingService :IScheduleResourceService
{
    private readonly IScheduleRepository<ScheduleResourceMapping> _resourceRepository;
    private readonly ILogger<ResourceMappingService> _logger;
    private readonly IMapper _mapper;
    public ResourceMappingService(IMapper mapper,
        IScheduleRepository<ScheduleResourceMapping> scheduleResourceRepository,ILogger<ResourceMappingService> logger)
    {
        _logger = logger?? throw new ArgumentNullException(nameof(logger));
        _resourceRepository = scheduleResourceRepository;
        _mapper = mapper;
    }
    
    public  async Task AddResourceMappingAsync(ScheduleResourceDto dto)
    {
        try
        {
            var resource = _mapper.Map<ScheduleResourceMapping>(dto);
            await _resourceRepository.AddAsync(resource);
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
            return entities.Select(e =>  _mapper.Map<ScheduleResourceDto>(e));
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
            var entity = await _resourceRepository.GetAsync(mappingId);
            if (entity != null)
            {
                _resourceRepository.Delete(entity);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resource mapping {MappingId}", mappingId);
            throw;
        }
    }
}