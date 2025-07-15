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
[ScheduleStrategy(ScheduleType.Yearly)]
internal class YearlyScheduleStrategy : IScheduleJobStrategy
{
    private readonly ILogger<YearlyScheduleStrategy> _logger;

    public YearlyScheduleStrategy(ILogger<YearlyScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.Yearly, name: "Yearly Schedule", description: "Executes tasks annually on specific dates");

    public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Yearly;

    public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var allJobIds = new List<string>();

            // Create cron expression for yearly execution
            var startDateTime = schedule.StartDateTime;
            var endDateTime = schedule.EndDateTime;

            // Yearly cron: "0 minute hour day month *" (runs every year)
            var startCron = $"0 {startDateTime.Minute} {startDateTime.Hour} {startDateTime.Day} {startDateTime.Month} *";
            var endCron = $"0 {endDateTime.Minute} {endDateTime.Hour} {endDateTime.Day} {endDateTime.Month} *";

            // Schedule start event
            var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
            var startResult = await scheduler.ScheduleCronAsync(topics, startTrigger, startCron, cancellationToken);
            
            if (!startResult.IsSuccess)
                return startResult;

            allJobIds.AddRange(startResult.ScheduledJobIds);

            // Schedule end event if different from start
            if (startCron != endCron)
            {
                var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
                var endResult = await scheduler.ScheduleCronAsync(topics, endTrigger, endCron, cancellationToken);
                
                if (!endResult.IsSuccess)
                    return endResult;

                allJobIds.AddRange(endResult.ScheduledJobIds);
            }

            _logger.LogInformation("Successfully scheduled yearly jobs for schedule {ScheduleId} on {Month}/{Day}", 
                schedule.Id, startDateTime.Month, startDateTime.Day);
            return ScheduleResult.Success(allJobIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in yearly schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in yearly schedule strategy", ex);
        }
    }
}