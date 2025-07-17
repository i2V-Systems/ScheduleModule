using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Microsoft.Extensions.Logging;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;


namespace Application.Schedule.ScheduleEvent.JobStratgies;

[TransientService]
[ScheduleStrategy(ScheduleType.Daily)]
internal class DailyScheduleStrategy : IScheduleJobStrategy
{
    private readonly ILogger<DailyScheduleStrategy> _logger;

    public DailyScheduleStrategy(ILogger<DailyScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Daily;

    public async Task<ScheduleResult> ScheduleJobAsync(
        ScheduleDto schedule, 
        IReadOnlyList<Resources> topics, 
        IUnifiedScheduler scheduler, 
        CancellationToken cancellationToken = default)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        if (topics == null) throw new ArgumentNullException(nameof(topics));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
        
        try
        {
            // Currently unsupported sub-type
            if (schedule.SubType == ScheduleSubType.Every)
            {
                _logger.LogWarning("Every N days logic not implemented for schedule {ScheduleId}", schedule.Id);
                return ScheduleResult.Failure("Every N days logic not implemented");
            }
            var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);

            if (schedule.EndDateTime.HasValue)
            {
                var endTime = TimeOnly.FromDateTime(schedule.EndDateTime.Value);
                return await ScheduleStartAndEndAsync(
                    topics,
                    async (t, trigger, time, _, ct) => await scheduler.ScheduleDailyAsync(t, trigger, time, ct),
                    schedule.Id,
                    startTime, null,
                    endTime, null,
                    cancellationToken);
            }
            else
            {
                // Only once event
                var onceTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
                var result = await scheduler.ScheduleDailyAsync(topics, onceTrigger, startTime, cancellationToken);
                return result.IsSuccess
                    ? ScheduleResult.Success(result.ScheduledJobIds)
                    : result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in daily schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in daily schedule strategy", ex);
        }
    }

    /// <summary>
    /// Generic method to schedule start & end events.
    /// </summary>
    private async Task<ScheduleResult> ScheduleStartAndEndAsync(
        IReadOnlyList<Resources> topics,
        Func<IReadOnlyList<Resources>, ScheduleEventTrigger, TimeOnly, string?, CancellationToken, Task<ScheduleResult>> scheduleFunc,
        Guid scheduleId,
        TimeOnly startTime, string? startCron,
        TimeOnly endTime, string? endCron,
        CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        // Start event
        var startTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.Start);
        var startResult = await scheduleFunc(topics, startTrigger, startTime, startCron, cancellationToken);
        if (!startResult.IsSuccess) return startResult;
        allJobIds.AddRange(startResult.ScheduledJobIds);

        // End event
        var endTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.End);
        var endResult = await scheduleFunc(topics, endTrigger, endTime, endCron, cancellationToken);
        if (!endResult.IsSuccess) return endResult;
        allJobIds.AddRange(endResult.ScheduledJobIds);

        return ScheduleResult.Success(allJobIds);
    }
}
