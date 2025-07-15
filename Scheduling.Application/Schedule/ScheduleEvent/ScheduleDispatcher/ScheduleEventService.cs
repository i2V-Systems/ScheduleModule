using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Validator;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;
namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher
{
    [TransientService]
    public class ScheduleEventService:IScheduleEventService
    {
        private readonly IScheduleStrategyFactory _strategyFactory;
        private readonly IScheduleValidator _validator;
        private readonly IUnifiedScheduler _scheduler;
        private readonly ILogger<ScheduleEventService> _logger;
        

        public ScheduleEventService( IScheduleStrategyFactory strategyFactory,
            IScheduleValidator validator,
            IUnifiedScheduler scheduler,
            ILogger<ScheduleEventService> logger)
        {

            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ScheduleResult> ExecuteAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(schedule);
            ArgumentNullException.ThrowIfNull(topics);
            try
            {
                // Validate schedule
                var scheduleValidation = _validator.ValidateSchedule(schedule);
                if (!scheduleValidation.IsValid)
                {
                    _logger.LogWarning("Schedule validation failed for schedule {ScheduleId}: {Errors}", 
                        schedule.Id,  scheduleValidation.Errors);
                    return ScheduleResult.Failure($"Schedule  validation failed: {string.Join(", ", scheduleValidation.Errors)}");
                }
                
                // Validate time range
                // var currentTime = DateTime.Now;
                // var timeValidation = _validator.ValidateTimeRange(currentTime, schedule.StartDateTime, schedule.EndDateTime);
            
                // if (!timeValidation.IsValid)
                // {
                //     _logger.LogInformation("Schedule execution skipped for schedule {ScheduleId}: {Reason}", 
                //         schedule.Id, timeValidation.Errors);
                //     return ScheduleResult.Failure($"Time validation failed: {string.Join(", ", timeValidation.Errors)}");
                // }
                // Get appropriate strategy and execute
                var strategy = _strategyFactory.GetStrategy(schedule.Type);
                var result = await strategy.ScheduleJobAsync(schedule, topics, _scheduler, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully scheduled {JobCount} jobs for schedule {ScheduleId}", 
                        result.ScheduledJobIds.Count, schedule.Id);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing schedule {ScheduleId}", schedule.Id);
                return ScheduleResult.Failure("An unexpected error occurred while scheduling jobs", ex);
            }
            
        }

        public async Task<ScheduleResult> UpdateAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, CancellationToken cancellationToken = default)
        {
            _logger.LogError("Not implemented");
            return ScheduleResult.Failure("An unexpected error occurred while scheduling jobs");
        }

        public async Task<ScheduleResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _logger.LogError("Not implemented");
            return ScheduleResult.Failure("An unexpected error occurred while scheduling jobs");
        }
        
    }

}
