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
[ScheduleStrategy(ScheduleType.DateWise)]
internal class DateWiseScheduleStrategy : IScheduleJobStrategy
{
     private readonly ILogger<DateWiseScheduleStrategy> _logger;

    public DateWiseScheduleStrategy(ILogger<DateWiseScheduleStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ScheduleTypeInfo SupportedType => new(ScheduleType.DateWise, name: "Date-wise Schedule", description: "Executes tasks on specific dates");

    public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.DateWise;

    public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
    {
        try
        {
            var allJobIds = new List<string>();

            var startDateTime = schedule.StartDateTime;
            var endDateTime = schedule.EndDateTime;

            // Schedule start event
            var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
            var startResult = await scheduler.ScheduleDateWiseAsync(topics, startTrigger, startDateTime, cancellationToken);
            
            if (!startResult.IsSuccess)
                return startResult;

            allJobIds.AddRange(startResult.ScheduledJobIds);

            // Schedule end event
            var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
            var endResult = await scheduler.ScheduleDateWiseAsync(topics, endTrigger, endDateTime, cancellationToken);
            
            if (!endResult.IsSuccess)
                return endResult;

            allJobIds.AddRange(endResult.ScheduledJobIds);

            _logger.LogInformation("Successfully scheduled date-wise jobs for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Success(allJobIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in date-wise schedule strategy for schedule {ScheduleId}", schedule.Id);
            return ScheduleResult.Failure("Error in date-wise schedule strategy", ex);
        }
    }
    
}
    