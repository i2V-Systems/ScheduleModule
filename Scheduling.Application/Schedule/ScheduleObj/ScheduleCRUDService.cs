using AutoMapper;
using Domain.Exceptions;
using Domain.Schedule;
using Microsoft.Extensions.Logging;
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
        public ScheduleCrudService(IMapper mapper,
           IScheduleRepository<Domain.Schedule.Schedule> scheduleRepository,ILogger<ScheduleCrudService> logger)
        {
            _logger = logger?? throw new ArgumentNullException(nameof(logger));
            _schedulesRepository = scheduleRepository;
            _mapper = mapper;
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
        public async Task<bool> ExistAsync(Guid id,string userName="")
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

        public async Task<ScheduleDto> AddAsync(ScheduleDto  dto,Guid userId, string userName = "")
        {
            try
            {
                _logger.LogInformation("Creating schedule {ScheduleName} by {UserName}", dto.Name, userName);

                var schedule = _mapper.Map<Domain.Schedule.Schedule>(dto);
                await _schedulesRepository.AddAsync(schedule,userId);

                _logger.LogInformation("Schedule created with ID {ScheduleId}", schedule.Id);
                dto= _mapper.Map<ScheduleDto>(schedule);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule {ScheduleName}", dto.Name);
                throw;
            }
        }
        
        public async Task DeleteAsync(Guid entityId,Guid userId, string userName = "")
        {
               try
            {
                _logger.LogInformation("Deleting schedule {ScheduleId} by {UserName}", entityId, userName);

                var entity = await _schedulesRepository.GetAsync(entityId);
                if (entity == null)
                    throw new NotFoundException($"Schedule with ID {entityId} not found");

                _schedulesRepository.Delete(entity,userId);

                _logger.LogInformation("Schedule {ScheduleId} deleted successfully", entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting schedule {ScheduleId}", entityId);
                throw;
            }
        }

        public async Task UpdateAsync(ScheduleDto dto,Guid userId, string userName = "")
        {
            try
            {
                _logger.LogInformation("Updating schedule {ScheduleId} by {UserName}", dto.Id, userName);
                var existingEntity = await _schedulesRepository.GetAsync(dto.Id);
                if (existingEntity == null)
                    throw new NotFoundException($"Schedule with ID {dto.Id} not found");

                // Update domain entity from DTO
                var schedule = _mapper.Map<Domain.Schedule.Schedule>(dto);
                _schedulesRepository.Update(schedule,userId);

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
