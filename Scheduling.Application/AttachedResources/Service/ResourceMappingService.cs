using Application.Schedule;
using AutoMapper;
using Domain.AttachedResources;
using Microsoft.AspNetCore.Http;
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
    private readonly Guid _userId;
    public ResourceMappingService(IMapper mapper,
        IScheduleRepository<ScheduleResourceMapping> scheduleResourceRepository,
        ILogger<ResourceMappingService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger?? throw new ArgumentNullException(nameof(logger));
        _resourceRepository = scheduleResourceRepository;
        _mapper = mapper;

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null &&
            httpContext.Request.Headers.TryGetValue("Userid", out var userId))
        {
            _userId = Guid.Parse(userId);
        }
    }

    public async Task<ScheduleResourceDto> AddResourceMappingAsync(ScheduleResourceDto dto)
    {
        try
        {
            var resource = _mapper.Map<Domain.AttachedResources.ScheduleResourceMapping>(dto);
            await _resourceRepository.AddAsync(resource,_userId);

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
    public  async Task<ScheduleResourceDto> UpdateResourceMappingAsync(ScheduleResourceDto dto)
    {
        try
        {
            var resource = _mapper.Map<ScheduleResourceMapping>(dto);
            _resourceRepository.Update(resource,_userId);

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
    public async Task DeleteResourceMappingAsync(Guid mappingId)
    {
        try
        {
            var entity = await _resourceRepository.GetAsync(mappingId);
            if (entity != null)
            {
                _resourceRepository.Delete(entity,_userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting resource mapping {MappingId}", mappingId);
            throw;
        }
    }

    public async Task<Guid> DeleteResourceSchdeuleMappingAsync(DetachScheduleResourceDto mapping)
    {
      try
      {
        var entity = await _resourceRepository.FindAsync(item => item.ScheduleId == mapping.ScheduleId && item.ResourceId == mapping.ResourceId);
        _resourceRepository.Delete(entity,_userId);
        return entity.Id;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in DeleteResourceSchdeuleMappingAsync of ResourceMappingService");
        throw;
      }
    }

    public async Task<string> GetAttachedResourceStringsAsync(Guid scheduleId)
    {
      var resources = await _resourceRepository.FindAllAsync(resource => resource.ScheduleId == scheduleId);

      return string.Join(", ",
        resources
          .Select(x => x.ResourceType)
          .Distinct());
    }

}
