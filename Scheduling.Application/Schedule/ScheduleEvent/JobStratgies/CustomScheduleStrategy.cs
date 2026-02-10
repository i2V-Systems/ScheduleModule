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

    public override async Task<ScheduleResult> ScheduleJobAsync(
      ScheduleDto schedule,
      IReadOnlyList<Resources> topics,
      IUnifiedScheduler scheduler,
      CancellationToken cancellationToken = default)
    {
      ValidateInputs(schedule, topics, scheduler);

      try
      {
        var startCron = BuildStartCron(schedule);

        return schedule.EndDateTime.HasValue
          ? await ScheduleStartAndEnd(schedule, topics, scheduler, startCron, cancellationToken)
          : await ScheduleOnce(schedule, topics, scheduler, startCron, cancellationToken);
      }
      catch (Exception exception)
      {
        _logger.LogError(exception,
          "Error in custom schedule strategy for schedule {ScheduleId}", schedule.Id);

        return ScheduleResult.Failure("Error in custom schedule strategy", exception);
      }
    }
    private string BuildStartCron(ScheduleDto schedule)
    {
      var cron = CronExpressionBuilder.BuildDailyCronExpression(schedule.StartDateTime);

      if (string.IsNullOrWhiteSpace(cron))
      {
        _logger.LogWarning(
          "Custom schedule {ScheduleId} missing cron expression", schedule.Id);

        throw new InvalidOperationException("Invalid start cron expression");
      }

      return cron;
    }


    /// <summary>
    /// Generic helper to schedule start & (if cron provided) end events.
    /// </summary>
    private async Task<ScheduleResult> ScheduleStartAndEndAsync(
        IReadOnlyList<Resources> topics,
        Func<IReadOnlyList<Resources>, ScheduleEventTrigger, DateTime, string?, CancellationToken, Task<ScheduleResult>> scheduleFunc,
        Guid scheduleId,
        ScheduleWindow startWindow,
        ScheduleWindow endWindow,
        CancellationToken cancellationToken)
    {
        var allJobIds = new List<string>();

        // Schedule start event
        var startTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.Start);
        var startResult = await scheduleFunc(topics, startTrigger, startWindow.DateTime, startWindow.Cron, cancellationToken);
        if (!startResult.IsSuccess) return startResult;
        allJobIds.AddRange(startResult.ScheduledJobIds);

        // Schedule end event only if endCron is provided and different from start
        if (!string.IsNullOrWhiteSpace(endWindow.Cron) && endWindow.Cron != startWindow.Cron)
        {
            var endTrigger = new ScheduleEventTrigger(scheduleId, ScheduleEventType.End);
            var endResult = await scheduleFunc(topics, endTrigger, endWindow.DateTime, endWindow.Cron, cancellationToken);
            if (!endResult.IsSuccess) return endResult;
            allJobIds.AddRange(endResult.ScheduledJobIds);
        }

        _logger.LogInformation("Successfully scheduled custom jobs for schedule {ScheduleId}", scheduleId);
        return ScheduleResult.Success(allJobIds);
    }
    private static void ValidateInputs(
      ScheduleDto schedule,
      IReadOnlyList<Resources> topics,
      IUnifiedScheduler scheduler)
    {
      ArgumentNullException.ThrowIfNull(schedule);
      ArgumentNullException.ThrowIfNull(topics);
      ArgumentNullException.ThrowIfNull(scheduler);
    }
    private async Task<ScheduleResult> ScheduleStartAndEnd(
      ScheduleDto schedule,
      IReadOnlyList<Resources> topics,
      IUnifiedScheduler scheduler,
      string startCron,
      CancellationToken ct)
    {
      var endCron = CronExpressionBuilder.BuildDailyCronExpression(
        schedule.EndDateTime!.Value);
      ScheduleWindow startTime = new ScheduleWindow(DateTime.MinValue, startCron);
      ScheduleWindow endTime = new ScheduleWindow(DateTime.MinValue, endCron);
      return await ScheduleStartAndEndAsync(
        topics,
        (list, trigger, _, cron, token) =>
          scheduler.ScheduleCronAsync(list, trigger, cron!, token),
        schedule.Id,
        startTime,
        endTime,
        ct);
    }
    private async Task<ScheduleResult> ScheduleOnce(
      ScheduleDto schedule,
      IReadOnlyList<Resources> topics,
      IUnifiedScheduler scheduler,
      string startCron,
      CancellationToken ct)
    {
      var trigger = CreateEventTrigger(schedule.Id, ScheduleEventType.Once);

      var result = await scheduler.ScheduleCronAsync(
        topics, trigger, startCron, ct);

      return result.IsSuccess
        ? ScheduleResult.Success(result.ScheduledJobIds)
        : result;
    }
    private static ScheduleEventTrigger CreateEventTrigger(
      Guid scheduleId,
      ScheduleEventType type)
    {
      return new ScheduleEventTrigger(scheduleId, type);
    }



}
