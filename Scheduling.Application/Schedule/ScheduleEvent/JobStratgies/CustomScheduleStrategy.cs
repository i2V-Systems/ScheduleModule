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
internal class CustomScheduleStrategy : IScheduleJobStrategy
{
    private readonly ILogger<CustomScheduleStrategy> _logger;

    public CustomScheduleStrategy(ILogger<CustomScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.Custom, name: "Custom Schedule", description: "Custom scheduling logic using cron expressions");

    public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Custom;

    public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var cronn = CronExpressionBuilder.BuildDailyCronExpression(schedule.StartDateTime);//TODO remove this and add custom logic 
            if (string.IsNullOrWhiteSpace(cronn))
            {
                _logger.LogWarning("Custom schedule {ScheduleId} missing cron expression", schedule.Id);
                return ScheduleResult.Failure("Custom schedule requires a valid cron expression");
            }

            var allJobIds = new List<string>();

            // Schedule start event with custom cron
            var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
            var startResult = await scheduler.ScheduleCronAsync(topics, startTrigger, cronn, cancellationToken);
            
            if (!startResult.IsSuccess)
                return startResult;

            allJobIds.AddRange(startResult.ScheduledJobIds);

            // Schedule end event if different cron expression provided
            // if (!string.IsNullOrWhiteSpace(schedule.EndCronExpression) && 
            //     schedule.EndCronExpression != schedule.CronExpression)
            // {
            //     var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
            //     var endResult = await scheduler.ScheduleCronAsync(topics, endTrigger, schedule.EndCronExpression, cancellationToken);
            //     
            //     if (!endResult.IsSuccess)
            //         return endResult;
            //
            //     allJobIds.AddRange(endResult.ScheduledJobIds);
            // }

            // _logger.LogInformation("Successfully scheduled custom jobs for schedule {ScheduleId} with cron {CronExpression}", 
            //     schedule.Id, schedule.CronExpression);
            return ScheduleResult.Success(allJobIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in custom schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in custom schedule strategy", ex);
        }
    }
}
