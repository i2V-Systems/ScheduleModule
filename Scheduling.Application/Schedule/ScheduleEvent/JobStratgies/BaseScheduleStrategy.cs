using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

namespace Application.Schedule.ScheduleEvent.JobStratgies;

public abstract class BaseScheduleJobStrategy : IScheduleJobStrategy
{
  public abstract Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default);

    public virtual async Task<ScheduleResult> UpdateJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
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
            return await ScheduleJobAsync(schedule, topics, scheduler, cancellationToken);
        }
        catch (Exception ex)
        {
            return ScheduleResult.Failure($"Failed to update schedule {schedule.Id}", ex);
        }
    }

    public virtual async Task<ScheduleResult> DeleteJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
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

    public virtual async Task<ScheduleResult> EnableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
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

    public virtual async Task<ScheduleResult> DisableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
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
    public abstract bool CanHandle(ScheduleType scheduleType);
}