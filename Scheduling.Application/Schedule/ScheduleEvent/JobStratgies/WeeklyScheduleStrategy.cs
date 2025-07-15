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
        try
        {
            return schedule.SubType switch
            {
                ScheduleSubType.Selecteddays => await ScheduleSelectedDaysAsync(schedule, topics, scheduler, cancellationToken),
                ScheduleSubType.Weekdays => await ScheduleWeekdaysAsync(schedule, topics, scheduler, cancellationToken),
                ScheduleSubType.Weekenddays => await ScheduleWeekendsAsync(schedule, topics, scheduler, cancellationToken),
                _ => ScheduleResult.Failure($"Unsupported weekly sub-type: {schedule.SubType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in weekly schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in weekly schedule strategy", ex);
        }
    }
     private async Task<ScheduleResult> ScheduleSelectedDaysAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        // Build cron expressions for selected days
        var startCronJob = CronExpressionBuilder.BuildCronExpression(schedule.StartDays, schedule.StartDateTime);
        var endCronJob = CronExpressionBuilder.BuildCronExpression(schedule.StartDays, schedule.EndDateTime);

        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
        var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);
        var startResult = await scheduler.ScheduleSelectedDaysAsync(topics, startTrigger, startTime, startCronJob, cancellationToken);
        
        if (!startResult.IsSuccess)
            return startResult;

        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event
        var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
        var endTime = TimeOnly.FromDateTime(schedule.EndDateTime);
        var endResult = await scheduler.ScheduleSelectedDaysAsync(topics, endTrigger, endTime, endCronJob, cancellationToken);
        
        if (!endResult.IsSuccess)
            return endResult;

        allJobIds.AddRange(endResult.ScheduledJobIds);

        return ScheduleResult.Success(allJobIds);
    }

    private async Task<ScheduleResult> ScheduleWeekdaysAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);
        var endTime = TimeOnly.FromDateTime(schedule.EndDateTime);

        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
        var startResult = await scheduler.ScheduleWeekDaysAsync(topics, startTrigger, startTime, cancellationToken);
        
        if (!startResult.IsSuccess)
            return startResult;

        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event
        var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
        var endResult = await scheduler.ScheduleWeekDaysAsync(topics, endTrigger, endTime, cancellationToken);
        
        if (!endResult.IsSuccess)
            return endResult;

        allJobIds.AddRange(endResult.ScheduledJobIds);

        return ScheduleResult.Success(allJobIds);
    }

    private async Task<ScheduleResult> ScheduleWeekendsAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);
        var endTime = TimeOnly.FromDateTime(schedule.EndDateTime);

        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
        var startResult = await scheduler.ScheduleWeekendDaysAsync(topics, startTrigger, startTime, cancellationToken);
        
        if (!startResult.IsSuccess)
            return startResult;

        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event
        var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
        var endResult = await scheduler.ScheduleWeekendDaysAsync(topics, endTrigger, endTime, cancellationToken);
        
        if (!endResult.IsSuccess)
            return endResult;

        allJobIds.AddRange(endResult.ScheduledJobIds);

        return ScheduleResult.Success(allJobIds);
    }
}
