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
[ScheduleStrategy(ScheduleType.Monthly)]
internal class MonthlyScheduleStrategy : BaseScheduleJobStrategy
{
    private readonly ILogger<MonthlyScheduleStrategy> _logger;

    public MonthlyScheduleStrategy(ILogger<MonthlyScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.Monthly, name: "Monthly Schedule", description: "Executes tasks monthly on specific days");

    public override bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Monthly;

    public override async Task<ScheduleResult> ScheduleJobAsync(
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
            var localStart = schedule.StartDateTime.Kind == DateTimeKind.Utc 
                ? schedule.StartDateTime.ToLocalTime() 
                : schedule.StartDateTime;
            var startDayOfMonth = localStart.Day;
            var startTime = TimeOnly.FromDateTime(localStart);

            if (schedule.EndDateTime.HasValue)
            {
                var localEnd = schedule.EndDateTime.Value.Kind == DateTimeKind.Utc 
                    ? schedule.EndDateTime.Value.ToLocalTime() 
                    : schedule.EndDateTime.Value;
                var endDayOfMonth = localEnd.Day;
                var endTime = TimeOnly.FromDateTime(localEnd);

                var allJobIds = new List<string>();

                // Schedule start event
                var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
                var startResult = await scheduler.ScheduleMonthlyAsync(topics, startTrigger, startDayOfMonth, startTime, cancellationToken);
                if (!startResult.IsSuccess) return startResult;
                allJobIds.AddRange(startResult.ScheduledJobIds);

                // Schedule end event if different from start
                if (startTime != endTime || startDayOfMonth != endDayOfMonth)
                {
                    var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
                    var endResult = await scheduler.ScheduleMonthlyAsync(topics, endTrigger, endDayOfMonth, endTime, cancellationToken);
                    if (!endResult.IsSuccess) return endResult;
                    allJobIds.AddRange(endResult.ScheduledJobIds);
                }

                _logger.LogInformation("Successfully scheduled monthly jobs for schedule {ScheduleId} on day {DayOfMonth}", schedule.Id, startDayOfMonth);
                return ScheduleResult.Success(allJobIds);
            }
            else
            {
                // Single start/once event
                var onceTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
                var result = await scheduler.ScheduleMonthlyAsync(topics, onceTrigger, startDayOfMonth, startTime, cancellationToken);
                return result.IsSuccess 
                    ? ScheduleResult.Success(result.ScheduledJobIds) 
                    : result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in monthly schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in monthly schedule strategy", ex);
        }
    }
}
