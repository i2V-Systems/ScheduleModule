using AutoMapper;
using Domain.Exceptions;
using Domain.Schedule;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.DTOs;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleObj
{
    [TransientService]
    internal class ScheduleCrudService : IScheduleCRUDService
    {
        private readonly IMapper _mapper;
        private IScheduleRepository<Domain.Schedule.Schedule> _schedulesRepository;
     
        private readonly ILogger<ScheduleCrudService> _logger;
        private readonly IScheduleAuditLogger? _auditLogger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Guid _userId;

    public ScheduleCrudService(
        IMapper mapper,
        IScheduleRepository<Domain.Schedule.Schedule> scheduleRepository,
        ILogger<ScheduleCrudService> logger,
        IHttpContextAccessor httpContextAccessor,
        IScheduleAuditLogger? auditLogger = null)
    {
      _logger = logger;
      _schedulesRepository = scheduleRepository;
      _mapper = mapper;
      _auditLogger = auditLogger;
      _httpContextAccessor = httpContextAccessor;

      var httpContext = httpContextAccessor.HttpContext;

      if (httpContext != null &&
          httpContext.Request.Headers.TryGetValue("Userid", out var userIdHeader) &&
          Guid.TryParse(userIdHeader, out var parsedGuid))
      {
        _userId = parsedGuid;
      }
      else
      {
        _userId = Guid.Empty;
      }
    }

    public async Task<ScheduleDto> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _schedulesRepository.GetAsync(id);
                return _mapper.Map<ScheduleDto>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting schedule {ScheduleId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<ScheduleDto>> GetAllAsync()
        {
            try
            {
                var entities = await _schedulesRepository.GetAllAsync();
                return entities.Select(e => _mapper.Map<ScheduleDto>(e));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all schedules");
                throw;
            }
        }
        public async Task<bool> ExistAsync(Guid id)
        {
            try
            {
                var entities = await _schedulesRepository.FindAsync(schedule =>schedule.Id==id);
                return entities != null ? true : false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all schedules");
                throw;
            }
        }

        public async Task<ScheduleDto> AddAsync(ScheduleDto  dto)
        {
            try
            {
                _logger.LogInformation("Creating schedule {ScheduleName} by {UserId}", dto.Name, _userId);

                var schedule = _mapper.Map<Domain.Schedule.Schedule>(dto);
                await _schedulesRepository.AddAsync(schedule, _userId);

                _logger.LogInformation("Schedule created with ID {ScheduleId}", schedule.Id);
                _auditLogger?.LogActivity("Add", dto.Name, GetExecutingUserName(), $"Schedule '{dto.Name}' created");
                dto= _mapper.Map<ScheduleDto>(schedule);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule {ScheduleName}", dto.Name);
                throw;
            }
        }
        
        public async Task DeleteAsync(Guid entityId)
        {
               try
            {
                _logger.LogInformation("Deleting schedule {ScheduleId} by {UserId}", entityId, _userId);

                var entity = await _schedulesRepository.GetAsync(entityId);
                if (entity == null)
                    throw new NotFoundException($"Schedule with ID {entityId} not found");

                _schedulesRepository.Delete(entity, _userId);

                _logger.LogInformation("Schedule {ScheduleId} deleted successfully", entityId);
                _auditLogger?.LogActivity("Delete", entity.Name, GetExecutingUserName(), $"Schedule '{entity.Name}' deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule {ScheduleId}", entityId);
                throw;
            }
        }

        public async Task UpdateAsync(ScheduleDto dto)
        {
            try
            {
                _logger.LogInformation("Updating schedule {ScheduleId} by {UserId}", dto.Id, _userId);
                var existingEntity = await _schedulesRepository.GetAsync(dto.Id);
                if (existingEntity == null)
                    throw new NotFoundException($"Schedule with ID {dto.Id} not found");

                // Update domain entity from DTO
                var schedule = _mapper.Map<Domain.Schedule.Schedule>(dto);
                _schedulesRepository.Update(schedule, _userId);

                _logger.LogInformation("Schedule {ScheduleId} updated successfully", dto.Id);
                _auditLogger?.LogActivity("Update", dto.Name, GetExecutingUserName(), $"Schedule '{dto.Name}' updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {ScheduleId}", dto.Id);
                throw;
            }
        }


        private string GetExecutingUserName()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Headers.TryGetValue("username", out var nameHeader) && !string.IsNullOrWhiteSpace(nameHeader))
                {
                    return nameHeader.ToString();
                }
                if (httpContext.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(httpContext.User.Identity.Name))
                {
                    return httpContext.User.Identity.Name;
                }
            }
            return "i2vadmin";
        }
    }
    
    
}
