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
[ScheduleStrategy(ScheduleType.Custom)]
internal class CustomScheduleStrategy : BaseScheduleJobStrategy
{
    private readonly ILogger<CustomScheduleStrategy> _logger;

    public CustomScheduleStrategy(ILogger<CustomScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.Custom, name: "Custom Schedule", description: "Custom scheduling logic using cron expressions");

    public override bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Custom;

    public override async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        if (schedule == null) throw new ArgumentNullException(nameof(schedule));
        if (topics == null) throw new ArgumentNullException(nameof(topics));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));
        try
        {
            var startCron = CronExpressionBuilder.BuildDailyCronExpression(schedule.StartDateTime);
            if (string.IsNullOrWhiteSpace(startCron))
            {
                _logger.LogWarning("Custom schedule {ScheduleId} missing cron expression", schedule.Id);
                return ScheduleResult.Failure("Custom schedule requires a valid cron expression");
            }
            if (schedule.EndDateTime.HasValue)
            {
                var endCron = schedule.EndDateTime!=null
                    ? CronExpressionBuilder.BuildDailyCronExpression(schedule.EndDateTime??DateTime.Now)
                    : null;
             
                return await ScheduleStartAndEndAsync(
                    topics,
                    async (t, trigger, _, cron, ct) => await scheduler.ScheduleCronAsync(t, trigger, cron!, ct),
                    schedule.Id,
                    DateTime.MinValue, startCron, // time unused in ScheduleCronAsync, so pass dummy
                    DateTime.MinValue, endCron,
                    cancellationToken);
            }
            else
            {
                // Only start/once event
                var trigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
                var result = await scheduler.ScheduleCronAsync(topics, trigger, startCron, cancellationToken);
                return result.IsSuccess 
                    ? ScheduleResult.Success(result.ScheduledJobIds)
                    : result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in custom schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in custom schedule strategy", ex);
        }
    }
    /// <summary>
    /// Generic helper to schedule start & (if cron provided) end events.
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

        // Schedule end event only if endCron is provided and different from start
        if (!string.IsNullOrWhiteSpace(endCron) && endCron != startCron)
        {
            var endTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.End);
            var endResult = await scheduleFunc(topics, endTrigger, endDateTime, endCron, cancellationToken);
            if (!endResult.IsSuccess) return endResult;
            allJobIds.AddRange(endResult.ScheduledJobIds);
        }

        _logger.LogInformation("Successfully scheduled custom jobs for schedule {ScheduleId}", scheduleId);
        return ScheduleResult.Success(allJobIds);
    }
}
