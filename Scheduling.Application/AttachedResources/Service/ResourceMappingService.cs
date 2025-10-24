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
    
    public async Task<ScheduleResourceDto> AddResourceMappingAsync(ScheduleResourceDto dto,Guid userId)
    {
        try
        {
            var resource = _mapper.Map<Domain.AttachedResources.ScheduleResourceMapping>(dto);
            await _resourceRepository.AddAsync(resource,userId);
            
            _logger.LogInformation("mapping created with ID {ScheduleId}", resource.Id);
            dto= _mapper.Map<ScheduleResourceDto>(resource);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding resource mapping");
            throw;
        }
    }
    public  async Task<ScheduleResourceDto> UpdateResourceMappingAsync(ScheduleResourceDto dto,Guid userId)
    {
        try
        {
            var resource = _mapper.Map<ScheduleResourceMapping>(dto);
            _resourceRepository.Update(resource,userId);
            
            _logger.LogInformation("mapping updated with ID {ScheduleId}", resource.Id);
            dto= _mapper.Map<ScheduleResourceDto>(resource);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating resource mapping");
            throw;
        }
    }
    public async Task<IEnumerable<ScheduleResourceDto>> GetAllResourceMappingAsync()
    {
        try
        {
            var entities = await _resourceRepository.GetAllAsync();
            var dtos = entities.Select(e =>  _mapper.Map<ScheduleResourceDto>(e));
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting resource mappings");
            throw;
        }
    }
    public async Task DeleteResourceMappingAsync(Guid mappingId,Guid userId)
    {
        try
        {
            var entity = await _resourceRepository.GetAsync(mappingId);
            if (entity != null)
            {
                _resourceRepository.Delete(entity,userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resource mapping {MappingId}", mappingId);
            throw;
        }
    }
}