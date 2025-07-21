using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Validator;
using Microsoft.Extensions.Logging;
using Quartz;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using Serilog;
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
                    Log.Error("Schedule validation failed for schedule {ScheduleId}: {Errors}", 
                        schedule.Id,  scheduleValidation.Errors);
                    return ScheduleResult.Failure($"Schedule  validation failed: {string.Join(", ", scheduleValidation.Errors)}");
                }
                var strategy = _strategyFactory.GetStrategy(schedule.Type);
                var result = await strategy.ScheduleJobAsync(schedule, topics, _scheduler, cancellationToken);

                if (result.IsSuccess)
                {
                    Log.Error("Successfully scheduled {JobCount} jobs for schedule {ScheduleId}", 
                        result.ScheduledJobIds.Count, schedule.Id);
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing schedule {ScheduleId}", schedule.Id);
                return ScheduleResult.Failure("An unexpected error occurred while scheduling jobs", ex);
            }
            
        }

        public async Task<ScheduleResult> UpdateAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(schedule);
            ArgumentNullException.ThrowIfNull(topics);
            try
            {
                // Validate schedule
                var scheduleValidation = _validator.ValidateSchedule(schedule);
                if (!scheduleValidation.IsValid)
                {
                    Log.Error("Schedule validation failed for schedule {ScheduleId}: {Errors}", 
                        schedule.Id,  scheduleValidation.Errors);
                    return ScheduleResult.Failure($"Schedule  validation failed: {string.Join(", ", scheduleValidation.Errors)}");
                }
                var strategy = _strategyFactory.GetStrategy(schedule.Type);
                var result = await strategy.UpdateJobAsync(schedule, topics, _scheduler, cancellationToken);

                if (result.IsSuccess)
                {
                    Log.Error("Successfully scheduled {JobCount} jobs for schedule {ScheduleId}", 
                        result.ScheduledJobIds.Count, schedule.Id);
                }
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing schedule {ScheduleId}", schedule.Id);
                return ScheduleResult.Failure("An unexpected error occurred while scheduling jobs", ex);
            }
        }

        public async Task<ScheduleResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try{
                var jobKeys = await _scheduler.GetJobKeysForScheduleAsync(id, cancellationToken);
                    
                if (!jobKeys.Any())
                {
                    Log.Warning("No jobs found for schedule {ScheduleId}", id);
                    return ScheduleResult.Success(new List<string>());
                }

                var success = await _scheduler.UnscheduleAllAsync(jobKeys, cancellationToken);
                    
                if (success)
                {
                    Log.Information("Successfully deleted {JobCount} jobs for schedule {ScheduleId}", 
                        jobKeys.Count, id);
                    return ScheduleResult.Success(jobKeys.ToList());
                }
                else
                {
                    Log.Error("Failed to delete jobs for schedule {ScheduleId}", id);
                    return ScheduleResult.Failure($"Failed to delete jobs for schedule {id}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting schedule {ScheduleId}", id);
                return ScheduleResult.Failure("An unexpected error occurred while deleting jobs", ex);
            }
        }
         public async Task<ScheduleResult> EnableAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _scheduler.ResumeJobAsync(id, cancellationToken);
                
                if (success)
                {
                    Log.Information("Successfully enabled schedule {ScheduleId}", id);
                    return ScheduleResult.Success(new List<string> { id.ToString() });
                }
                else
                {
                    Log.Error("Failed to enable schedule {ScheduleId}", id);
                    return ScheduleResult.Failure($"Failed to enable schedule {id}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error enabling schedule {ScheduleId}", id);
                return ScheduleResult.Failure("An unexpected error occurred while enabling schedule", ex);
            }
        }

        public async Task<ScheduleResult> DisableAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var success = await _scheduler.PauseJobAsync(id, cancellationToken);
                
                if (success)
                {
                    Log.Information("Successfully disabled schedule {ScheduleId}", id);
                    return ScheduleResult.Success(new List<string> { id.ToString() });
                }
                else
                {
                    Log.Error("Failed to disable schedule {ScheduleId}", id);
                    return ScheduleResult.Failure($"Failed to disable schedule {id}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error disabling schedule {ScheduleId}", id);
                return ScheduleResult.Failure("An unexpected error occurred while disabling schedule", ex);
            }
        }

        public async Task<ScheduleResult> GetScheduleStatusAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            { 
                var status = await _scheduler.GetScheduleStatusAsync(id, cancellationToken);
                if (status == ScheduleStatus.NotFound)
                {
                    return ScheduleResult.Failure($"No jobs found for schedule {id}");
                }
                var statusInfo = new List<string> { $"Status: {status}" };
                var nextExecution = await _scheduler.GetNextExecutionTimeAsync(id, cancellationToken);
                if (nextExecution.HasValue)
                {
                    statusInfo.Add($"Next Execution: {nextExecution:yyyy-MM-dd HH:mm:ss UTC}");
                }
                return ScheduleResult.Success(statusInfo);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting status for schedule {ScheduleId}", id);
                return ScheduleResult.Failure("An unexpected error occurred while getting schedule status", ex);
            }
        }
        public async Task<bool> IsScheduleEnabledAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _scheduler.IsScheduleActiveAsync(id, cancellationToken);
        }
        
        public async Task<bool> ScheduleExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var resList=  await _scheduler.GetJobKeysForScheduleAsync(id,cancellationToken);
            if (resList.Count > 0)
                return true;
            return false;
        }
    }

}
