using Domain.Exceptions;
using Domain.Schedule;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.Schedule.ScheduleObj
{
    public class ScheduleCrudService : IScheduleCRUDService
    {
        private readonly IScheduleRepository _schedulesRepository;
     
        private readonly ILogger<ScheduleCrudService> _logger;
        public ScheduleCrudService(
           IScheduleRepository scheduleRepository,ILogger<ScheduleCrudService> logger)
        {
            _logger = logger?? throw new ArgumentNullException(nameof(logger));
            _schedulesRepository = scheduleRepository;
        }

        public async Task<ScheduleDto> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _schedulesRepository.GetByIdAsync(id);
                return entity?.ToDto();
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
                return entities.Select(e => e.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all schedules");
                throw;
            }
        }

        public async Task<Guid> AddAsync(ScheduleDto  dto, string userName = "")
        {
            try
            {
                _logger.LogInformation("Creating schedule {ScheduleName} by {UserName}", dto.Name, userName);

                // Convert DTO to domain entity
                var entity = dto.ToDomain();
                
                await _schedulesRepository.AddAsync(entity);

                _logger.LogInformation("Schedule created with ID {ScheduleId}", entity.Id);
                return entity.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule {ScheduleName}", dto.Name);
                throw;
            }
        }
        
        public async Task DeleteAsync(Guid entityId, string userName = "")
        {
               try
            {
                _logger.LogInformation("Deleting schedule {ScheduleId} by {UserName}", entityId, userName);

                var entity = await _schedulesRepository.GetByIdAsync(entityId);
                if (entity == null)
                    throw new NotFoundException($"Schedule with ID {entityId} not found");

                await _schedulesRepository.DeleteAsync(entityId);

                _logger.LogInformation("Schedule {ScheduleId} deleted successfully", entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule {ScheduleId}", entityId);
                throw;
            }
        }

        public async Task UpdateAsync(ScheduleDto dto, string userName = "")
        {
            try
            {
                _logger.LogInformation("Updating schedule {ScheduleId} by {UserName}", dto.Id, userName);
                var existingEntity = await _schedulesRepository.GetByIdAsync(dto.Id);
                if (existingEntity == null)
                    throw new NotFoundException($"Schedule with ID {dto.Id} not found");

                // Update domain entity from DTO
                existingEntity.UpdateFromDto(dto);
                await _schedulesRepository.UpdateAsync(existingEntity);

                _logger.LogInformation("Schedule {ScheduleId} updated successfully", dto.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {ScheduleId}", dto.Id);
                throw;
            }
        }
    }
    
    
}
