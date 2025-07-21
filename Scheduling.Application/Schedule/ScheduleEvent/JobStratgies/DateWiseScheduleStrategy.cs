using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;


namespace Application.Schedule.ScheduleEvent.JobStratgies;

[TransientService]
[ScheduleStrategy(ScheduleType.DateWise)]
internal class DateWiseScheduleStrategy : BaseScheduleJobStrategy
{
     private readonly ILogger<DateWiseScheduleStrategy> _logger;

    public DateWiseScheduleStrategy(ILogger<DateWiseScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.DateWise, name: "Date-wise Schedule", description: "Executes tasks on specific dates");

    public override bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.DateWise;

    public override async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        if (topics == null) throw new ArgumentNullException(nameof(topics));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
        try
        {
            if (schedule.EndDateTime.HasValue)
            {
                return await ScheduleStartAndEndAsync(
                    topics,
                    async (t, trigger, dt, _, ct) => await scheduler.ScheduleDateWiseAsync(t, trigger, dt, ct),
                    schedule.Id,
                    schedule.StartDateTime, null,
                    schedule.EndDateTime??DateTime.Now, null,
                    cancellationToken);
            }
            else
            {
                // Only start/once event
                var trigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
                var result = await scheduler.ScheduleDateWiseAsync(topics, trigger, schedule.StartDateTime, cancellationToken);
                return result.IsSuccess 
                    ? ScheduleResult.Success(result.ScheduledJobIds)
                    : result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in date-wise schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in date-wise schedule strategy", ex);
        }
    }
    
    public async Task<ScheduleResult> UpdateJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, get existing job keys for this schedule
            var existingJobKeys = await scheduler.GetJobKeysForScheduleAsync(schedule.Id, cancellationToken);
            // Delete existing jobs
            if (existingJobKeys.Any())
            {
                await scheduler.UnscheduleAllAsync(existingJobKeys, cancellationToken);
            }
            // Create new jobs with updated configuration
            var result = await ScheduleJobAsync(schedule, topics, scheduler, cancellationToken);
            if (result.IsSuccess)
            {
                Log.Information("Successfully updated daily schedule {ScheduleId} for {TopicCount} topics", 
                    schedule.Id, topics.Count);
            }
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update daily schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Failed to update daily schedule", ex);
        }
    }

    public async Task<ScheduleResult> DeleteJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKeys = await scheduler.GetJobKeysForScheduleAsync(scheduleId, cancellationToken);
            if (!jobKeys.Any())
            {
                return ScheduleResult.Success(new List<string>());
            }
            var success = await scheduler.UnscheduleAllAsync(jobKeys, cancellationToken);
            return success 
                ? ScheduleResult.Success(jobKeys.ToList()) 
                : ScheduleResult.Failure($"Failed to delete some jobs for schedule {scheduleId}");
        }
        catch (Exception ex)
        {
            return ScheduleResult.Failure($"Failed to delete schedule {scheduleId}", ex);
        }
    }

    public async Task<ScheduleResult> EnableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await scheduler.ResumeJobAsync(scheduleId, cancellationToken);
            return success 
                ? ScheduleResult.Success(new List<string> { scheduleId.ToString() }) 
                : ScheduleResult.Failure($"Failed to enable schedule {scheduleId}");
        }
        catch (Exception ex)
        {
            return ScheduleResult.Failure($"Failed to enable schedule {scheduleId}", ex);
        }
    }

    public async Task<ScheduleResult> DisableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await scheduler.PauseJobAsync(scheduleId, cancellationToken);
            
            return success 
                ? ScheduleResult.Success(new List<string> { scheduleId.ToString() }) 
                : ScheduleResult.Failure($"Failed to disable schedule {scheduleId}");
        }
        catch (Exception ex)
        {
            return ScheduleResult.Failure($"Failed to disable schedule {scheduleId}", ex);
        }
    }
    
    /// <summary>
    /// Generic helper to schedule start & end events.
    /// </summary>
    private async Task<ScheduleResult> ScheduleStartAndEndAsync(
        IReadOnlyList<Resources> topics,
        Func<IReadOnlyList<Resources>, ScheduleEventTrigger, DateTime, string?, CancellationToken, Task<ScheduleResult>> scheduleFunc,
        Guid scheduleId,
        DateTime startDateTime, string? startCron,
        DateTime endDateTime, string? endCron,
        CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.Start);
        var startResult = await scheduleFunc(topics, startTrigger, startDateTime, startCron, cancellationToken);
        if (!startResult.IsSuccess) return startResult;
        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event
        var endTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.End);
        var endResult = await scheduleFunc(topics, endTrigger, endDateTime, endCron, cancellationToken);
        if (!endResult.IsSuccess) return endResult;
        allJobIds.AddRange(endResult.ScheduledJobIds);

        _logger.LogInformation("Successfully scheduled date-wise jobs for schedule {ScheduleId}", scheduleId);
        return ScheduleResult.Success(allJobIds);
    }
    
}
    