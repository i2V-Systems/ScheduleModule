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

    public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<ScheduleResult>();

            if (schedule.SubType == ScheduleSubType.Every)
            {
                // TODO: Implement every N days logic
                _logger.LogWarning("Every N days logic not implemented for schedule {ScheduleId}", schedule.Id);
                return ScheduleResult.Failure("Every N days logic not implemented");
            }

            // Schedule start and end events
            var startResult = await ScheduleStartAndEndEventsAsync(schedule, topics, scheduler, cancellationToken);
            return startResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in daily schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in daily schedule strategy", ex);
        }
    }

    private async Task<ScheduleResult> ScheduleStartAndEndEventsAsync(
        ScheduleDto schedule, 
        IReadOnlyList<Resources> topics, 
        IUnifiedScheduler scheduler,
        CancellationToken cancellationToken)
    {
        var startTime = TimeOnly.FromDateTime( schedule.StartDateTime);
        var endTime = TimeOnly.FromDateTime( schedule.StartDateTime);
        var allJobIds = new List<string>();

        
        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
        var startResult = await scheduler.ScheduleDailyAsync(topics, startTrigger, startTime, cancellationToken);
        
        if (!startResult.IsSuccess)
            return startResult;

        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event
        var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
        var endResult = await scheduler.ScheduleDailyAsync(topics, endTrigger, endTime, cancellationToken);
        
        if (!endResult.IsSuccess)
            return endResult;

        allJobIds.AddRange(endResult.ScheduledJobIds);

        return ScheduleResult.Success(allJobIds);
    }
}
