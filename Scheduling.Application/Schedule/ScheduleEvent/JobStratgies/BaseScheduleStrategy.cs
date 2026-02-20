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
        catch (Exception exception)
        {
            return ScheduleResult.Failure($"Failed to update schedule {schedule.Id}", exception);
        }
    }

    public virtual async Task<ScheduleResult> DeleteJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKeys = await scheduler.GetJobKeysForScheduleAsync(scheduleId, cancellationToken);

            if (!jobKeys.Any())
            {              List<string> emptyList = new List<string>();

                return ScheduleResult.Success(emptyList);
            }

            var success = await scheduler.UnscheduleAllAsync(jobKeys, cancellationToken);
            List<string> list = jobKeys.ToList();

            return success
                ? ScheduleResult.Success(list)
                : ScheduleResult.Failure($"Failed to delete some jobs for schedule {scheduleId}");
        }
        catch (Exception exception)
        {
            return ScheduleResult.Failure($"Failed to delete schedule {scheduleId}", exception);
        }
    }

    public virtual async Task<ScheduleResult> EnableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await scheduler.ResumeJobAsync(scheduleId, cancellationToken);
          List<string> scheduleIdList =new List<string> { scheduleId.ToString() };
            return success
                ? ScheduleResult.Success(scheduleIdList)
                : ScheduleResult.Failure($"Failed to enable schedule {scheduleId}");
        }
        catch (Exception exception)
        {
            return ScheduleResult.Failure($"Failed to enable schedule {scheduleId}", exception);
        }
    }

    public virtual async Task<ScheduleResult> DisableJobAsync(Guid scheduleId, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await scheduler.PauseJobAsync(scheduleId, cancellationToken);
            List<string> scheduleKeys = new List<string> { scheduleId.ToString() };
            return success
                ? ScheduleResult.Success(scheduleKeys)
                : ScheduleResult.Failure($"Failed to disable schedule {scheduleId}");
        }
        catch (Exception exception)
        {
            return ScheduleResult.Failure($"Failed to disable schedule {scheduleId}", exception);
        }
    }
    public abstract bool CanHandle(ScheduleType scheduleType);
    public sealed record ScheduleWindow(
      DateTime DateTime,
      string? Cron);
    public sealed record ScheduleWindowTimeOnly(
      TimeOnly DateTime,
      string? Cron);
}
