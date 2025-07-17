using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
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
[ScheduleStrategy(ScheduleType.Weekly)]
internal class WeeklyScheduleStrategy : IScheduleJobStrategy
{
    private readonly ILogger<WeeklyScheduleStrategy> _logger;

    public WeeklyScheduleStrategy(ILogger<WeeklyScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public ScheduleTypeInfo SupportedType => new(ScheduleType.Weekly, name: "Weekly Schedule", description: "Executes tasks on specific days of the week");
    
    public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Weekly;

    public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        if (topics == null) throw new ArgumentNullException(nameof(topics));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

        try
        {
            return schedule.SubType switch
            {
                ScheduleSubType.Selecteddays => await ScheduleSelectedDaysAsync(schedule, topics, scheduler, cancellationToken),
                ScheduleSubType.Weekdays => await  ScheduleWithStrategyAsync(schedule, topics, scheduler.ScheduleWeekDaysAsync, cancellationToken),
                ScheduleSubType.Weekenddays => await ScheduleWithStrategyAsync(schedule, topics, scheduler.ScheduleWeekendDaysAsync, cancellationToken),
                _ => ScheduleResult.Failure($"Unsupported weekly sub-type: {schedule.SubType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in weekly schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in weekly schedule strategy", ex);
        }
    }
    /// <summary>
    /// Handles scheduling for selected days using cron expressions.
    /// </summary>
     private async Task<ScheduleResult> ScheduleSelectedDaysAsync(
        ScheduleDto schedule, 
        IReadOnlyList<Resources> topics, 
        IUnifiedScheduler scheduler, 
        CancellationToken cancellationToken)
    {
        
        var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);
        var startCron = CronExpressionBuilder.BuildCronExpression(schedule.StartDays, schedule.StartDateTime);

        if (schedule.EndDateTime.HasValue)
        {
            var endTime = TimeOnly.FromDateTime(schedule.EndDateTime.Value);
            var endCron = CronExpressionBuilder.BuildCronExpression(schedule.StartDays, schedule.EndDateTime.Value);
            return await ScheduleStartAndEndAsync(
                topics,
                scheduler.ScheduleSelectedDaysAsync,
                schedule.Id,
                startTime, startCron,
                endTime, endCron,
                cancellationToken);
        }
        else
        {
            // Only start/once event
            var trigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
            var result = await scheduler.ScheduleSelectedDaysAsync(topics, trigger, startTime, startCron, cancellationToken);
            return result.IsSuccess 
                ? ScheduleResult.Success(result.ScheduledJobIds)
                : result;
        }
    }
    
    /// <summary>
    /// Generic method to schedule start & end events for weekday/weekend etc.
    /// </summary>
    private async Task<ScheduleResult> ScheduleWithStrategyAsync(
        ScheduleDto schedule,
        IReadOnlyList<Resources> topics,
        Func<IReadOnlyList<Resources>, ScheduleEventTrigger, TimeOnly, CancellationToken, Task<ScheduleResult>> scheduleFunc,
        CancellationToken cancellationToken)
    {
        var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);

        if (schedule.EndDateTime.HasValue)
        {
            var endTime = TimeOnly.FromDateTime(schedule.EndDateTime.Value);

            return await ScheduleStartAndEndAsync(
                topics,
                async (t, trigger, time, _, ct) => await scheduleFunc(t, trigger, time, ct),
                schedule.Id,
                startTime, null,
                endTime, null,
                cancellationToken);
        }
        else
        {
            // Only start/once event
            var trigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
            var result = await scheduleFunc(topics, trigger, startTime, cancellationToken);
            return result.IsSuccess 
                ? ScheduleResult.Success(result.ScheduledJobIds)
                : result;
        }
    }
    /// <summary>
    /// Schedules both start & end events using a common delegate.
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
