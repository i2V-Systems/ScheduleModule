using System.Globalization;
using Application.Schedule.ScheduleEvent.JobKey;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
using Serilog;

namespace Application.Schedule.ScheduleEvent.Scheduler;

public class QuartzUnifiedScheduler :IUnifiedScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IJobKeyGenerator _jobKeyGenerator;
    private readonly ILogger<QuartzUnifiedScheduler> _logger;

    public QuartzUnifiedScheduler(   ISchedulerFactory schedulerFactory,
        IJobKeyGenerator jobKeyGenerator,
        ILogger<QuartzUnifiedScheduler> logger)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _jobKeyGenerator = jobKeyGenerator ?? throw new ArgumentNullException(nameof(jobKeyGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ScheduleResult> ScheduleDailyAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, TimeOnly time, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcTimeZone = TimeZoneInfo.Utc;
            var startingAt = TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute);
            return await ScheduleJobsAsync(topics, metadata, trigger =>
                    trigger.WithDailyTimeIntervalSchedule(dailyTimeIntervalScheduleBuilder => dailyTimeIntervalScheduleBuilder
                        .StartingDailyAt(startingAt)
                        .OnEveryDay()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionFireAndProceed())
                    , cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to schedule daily jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule daily jobs", exception);
        }
    }

    private async Task<ScheduleResult> ScheduleJobsAsync(
        IReadOnlyList<Resources> topics,
        ScheduleEventTrigger metadata,
        Func<TriggerBuilder, TriggerBuilder> configureTrigger,
        CancellationToken cancellationToken)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            // Use deterministic job key
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss",CultureInfo.InvariantCulture);
            var jobKey = $"schedule-{metadata.scheduleId}-{metadata.eventType}-{timestamp}";
            var triggerKey = $"trigger-{metadata.scheduleId}-{metadata.eventType}-{timestamp}";
            var topicsList = topics.Select(resources => resources.ToString()).ToList();
            var topicsJson = System.Text.Json.JsonSerializer.Serialize(topicsList);

            var jobData = new JobDataMap
            {
                { "scheduleId", metadata.scheduleId.ToString() },
                { "eventType", metadata.eventType.ToString() },
                { "topics" , topicsJson},
                { "originalTopicCount", topics.Count },
                { "createdAt", DateTime.UtcNow.ToString("O") }, // ISO 8601 format
                { "jobKey", jobKey }
            };

            var job = JobBuilder.Create<TopicDispatcherJob>()
                .WithIdentity(jobKey,"DEFAULT")
                .RequestRecovery(true) // Enable recovery
                .StoreDurably(true)   // Keep job even if no triggers
                .SetJobData(jobData)
                .Build();

            // Configure the trigger
            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(triggerKey, "DEFAULT")  // ← ADD GROUP HERE
                .ForJob(jobKey, "DEFAULT"); // ← SPECIFY JOB GROUP
            var trigger = configureTrigger(triggerBuilder).Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            Log.Error("Scheduled job {JobKey} for schedule {ScheduleId} with {TopicCount} topics",
                jobKey, metadata.scheduleId, topics.Count);
            List<string> jobKeyList= new List<string> { jobKey };
            return ScheduleResult.Success(jobKeyList);

        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to schedule jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule jobs", exception);
        }
    }

    public async Task<ScheduleResult> ScheduleWeekDaysAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, TimeOnly time, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcTimeZone = TimeZoneInfo.Utc;
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
               var timeFormated= TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey, "DEFAULT")  // ← ADD GROUP HERE
                    .ForJob(jobKey, "DEFAULT")
                    .WithDailyTimeIntervalSchedule(dailyTimeIntervalScheduleBuilder => dailyTimeIntervalScheduleBuilder
                        .StartingDailyAt(timeFormated)
                        .OnMondayThroughFriday()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionFireAndProceed())
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception exception)
        {
           Log.Error(exception, "Failed to schedule weekday jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule weekday jobs", exception);
        }
    }

    public async Task<ScheduleResult> ScheduleWeekendDaysAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, TimeOnly time, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcTimeZone = TimeZoneInfo.Utc;
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var timeFormated = TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey,"DEFAULT")
                    .ForJob(jobKey, "DEFAULT")
                    .WithDailyTimeIntervalSchedule(dailyTimeIntervalScheduleBuilder => dailyTimeIntervalScheduleBuilder
                        .StartingDailyAt(timeFormated)
                        .OnSaturdayAndSunday()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionFireAndProceed())
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to schedule weekend jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule weekend jobs", exception);
        }
    }

    public async Task<ScheduleResult> ScheduleSelectedDaysAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, TimeOnly time, string cronExpression, CancellationToken cancellationToken = default)
    {
        return await ScheduleCronAsync(topics, metadata, cronExpression, cancellationToken);
    }

    public async Task<ScheduleResult> ScheduleDateWiseAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, DateTime executeAt, CancellationToken cancellationToken = default)
    {
        return await ScheduleOnceAsync(topics, metadata, executeAt, cancellationToken);
    }

    public async Task<ScheduleResult> ScheduleMonthlyAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, int day, TimeOnly time, CancellationToken cancellationToken = default)
    {
        var cronExpression = $"0 {time.Minute} {time.Hour} {day} * ?";
        return await ScheduleCronAsync(topics, metadata, cronExpression, cancellationToken);
    }

    public async Task<ScheduleResult> ScheduleCronAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, string cronExpression, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcTimeZone = TimeZoneInfo.Utc;

            return await ScheduleJobsAsync(topics, metadata, trigger =>
                        trigger.WithCronSchedule(cronExpression, cronScheduleBuilder => cronScheduleBuilder
                            .InTimeZone(utcTimeZone)
                            .WithMisfireHandlingInstructionFireAndProceed())
                    , cancellationToken);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to schedule cron jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule cron jobs", exception);
        }
    }

    public async Task<ScheduleResult> ScheduleOnceAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, DateTime executeAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var utcTimeZone = TimeZoneInfo.Utc;
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey, "DEFAULT")
                    .ForJob(jobKey, "DEFAULT")
                    .StartAt(executeAt)
                    .WithSimpleSchedule(simpleScheduleBuilder=>simpleScheduleBuilder
                        .WithRepeatCount(0)
                    )
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to schedule one-time jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule one-time jobs", exception);
        }
    }

    public async Task<bool> UnscheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKey = new Quartz.JobKey(jobId);
            return await scheduler.DeleteJob(jobKey, cancellationToken);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to unschedule job {JobId}", jobId);
            return false;
        }
    }

    public async Task<bool> UnscheduleAllAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKeys = jobIds.Select(id => new Quartz.JobKey(id)).ToList();
            return await scheduler.DeleteJobs(jobKeys, cancellationToken);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to unschedule multiple jobs");
            return false;
        }
    }

    private static IJobDetail CreateJob(string jobKey, ScheduleEventTrigger metadata)
    {
      string metaDataString= metadata.eventType.ToString();
        return JobBuilder.Create<TopicDispatcherJob>()
            .WithIdentity(jobKey, "DEFAULT")
            .RequestRecovery(true) // Enable recovery
            .StoreDurably(true)   // Keep job even if no triggers
            .UsingJobData("scheduleId", metadata.scheduleId)
            .UsingJobData("EventType", metaDataString)
            .Build();
    }

    // method to support updating schedules
    public async Task<ScheduleResult> UpdateScheduleAsync(Guid scheduleId, IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, Func<TriggerBuilder, TriggerBuilder> configureTrigger, CancellationToken cancellationToken = default)
    {
        try
        {
            // First, remove existing jobs for this schedule
            var existingJobKeys = await GetJobKeysForScheduleAsync(scheduleId, cancellationToken);
            if (existingJobKeys.Any())
            {
                await UnscheduleAllAsync(existingJobKeys, cancellationToken);
                Log.Information("Removed {JobCount} existing jobs for schedule {ScheduleId}", existingJobKeys.Count, scheduleId);
            }

            // Create new jobs with updated configuration
            return await ScheduleJobsAsync(topics, metadata, configureTrigger, cancellationToken);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to update schedule {ScheduleId}", scheduleId);
            return ScheduleResult.Failure("Failed to update schedule", exception);
        }
    }
    public async Task<bool> PauseJobAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKeys = await GetJobKeysForScheduleAsync(scheduleId, cancellationToken);

            foreach (var jobKeyString in jobKeys)
            {
                var jobKey = new Quartz.JobKey(jobKeyString);
                await scheduler.PauseJob(jobKey, cancellationToken);
            }

            Log.Information("Paused {JobCount} jobs for schedule {ScheduleId}", jobKeys.Count, scheduleId);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to pause jobs for schedule {ScheduleId}", scheduleId);
            return false;
        }
    }
    public async Task<bool> ResumeJobAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKeys = await GetJobKeysForScheduleAsync(scheduleId, cancellationToken);

            foreach (var jobKeyString in jobKeys)
            {
                var jobKey = new Quartz.JobKey(jobKeyString);
                await scheduler.ResumeJob(jobKey, cancellationToken);
            }

            Log.Information("Resumed {JobCount} jobs for schedule {ScheduleId}", jobKeys.Count, scheduleId);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to resume jobs for schedule {ScheduleId}", scheduleId);
            return false;
        }
    }
    public async Task<IReadOnlyList<string>> GetJobKeysForScheduleAsync(
      Guid scheduleId,
      CancellationToken cancellationToken = default)
    {
      try
      {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobKeys = await GetAllJobKeysAsync(scheduler, cancellationToken);

        var matchingJobKeys = new List<string>();

        foreach (var jobKey in jobKeys)
        {
          if (JobKeyMatchesSchedule(jobKey, scheduleId))
          {
            matchingJobKeys.Add(jobKey.Name);
            continue;
          }

          if (await JobDataMatchesScheduleAsync(
                scheduler,
                jobKey,
                scheduleId,
                cancellationToken))
          {
            matchingJobKeys.Add(jobKey.Name);
          }
        }

        return matchingJobKeys.AsReadOnly();
      }
      catch (Exception exception)
      {
        Log.Error(
          exception,
          "Failed to get job keys for schedule {ScheduleId}",
          scheduleId);

        return Array.Empty<string>();
      }
    }
    private static async Task<IReadOnlyCollection<Quartz.JobKey>> GetAllJobKeysAsync(
      IScheduler scheduler,
      CancellationToken cancellationToken)
    {
      var groupMatcher = GroupMatcher<Quartz.JobKey>.AnyGroup();
      return await scheduler.GetJobKeys(groupMatcher, cancellationToken);
    }
    private static bool JobKeyMatchesSchedule(
      Quartz.JobKey jobKey,
      Guid scheduleId)
    {
      return jobKey.Name.Contains($"schedule-{scheduleId}");
    }
    private static async Task<bool> JobDataMatchesScheduleAsync(
      IScheduler scheduler,
      Quartz.JobKey jobKey,
      Guid scheduleId,
      CancellationToken cancellationToken)
    {
      var jobDetail = await scheduler.GetJobDetail(jobKey, cancellationToken);
      if (jobDetail?.JobDataMap.ContainsKey("scheduleId") != true)
        return false;

      var jobScheduleId = jobDetail.JobDataMap.GetString("scheduleId");

      return Guid.TryParse(jobScheduleId, out var parsedScheduleId) &&
             parsedScheduleId == scheduleId;
    }

    public async Task<ScheduleStatus> GetScheduleStatusAsync(
      Guid scheduleId,
      CancellationToken cancellationToken = default)
    {
      try
      {
        var jobKeys = await GetJobKeysForScheduleAsync(
          scheduleId,
          cancellationToken);

        if (!jobKeys.Any())
          return ScheduleStatus.NotFound;

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var status = await EvaluateScheduleStatus(
          scheduler,
          jobKeys,
          cancellationToken);

        return status;
      }
      catch (Exception exception)
      {
        Log.Error(
          exception,
          "Error getting status for schedule {ScheduleId}",
          scheduleId);

        return ScheduleStatus.NotFound;
      }
    }
    private static async Task<ScheduleStatus> EvaluateScheduleStatus(
      IScheduler scheduler,
      IEnumerable<string> jobKeys,
      CancellationToken cancellationToken)
    {
      var allPaused = true;
      var hasActiveTriggers = false;

      foreach (var jobKeyString in jobKeys)
      {
        var jobKey = new Quartz.JobKey(jobKeyString);
        var triggers = await scheduler.GetTriggersOfJob(
          jobKey,
          cancellationToken);

        foreach (var trigger in triggers)
        {
          var state = await scheduler.GetTriggerState(
            trigger.Key,
            cancellationToken);

          UpdateTriggerFlags(state, ref allPaused, ref hasActiveTriggers);
        }
      }

      return ResolveScheduleStatus(allPaused, hasActiveTriggers);
    }
    private static void UpdateTriggerFlags(
      TriggerState state,
      ref bool allPaused,
      ref bool hasActiveTriggers)
    {
      if (state != TriggerState.Paused)
        allPaused = false;

      if (state == TriggerState.Normal)
        hasActiveTriggers = true;
    }
    private static ScheduleStatus ResolveScheduleStatus(
      bool allPaused,
      bool hasActiveTriggers)
    {
      if (allPaused)
        return ScheduleStatus.Disabled;

      if (hasActiveTriggers)
        return ScheduleStatus.Enabled;

      return ScheduleStatus.Disabled;
    }

    public async Task<bool> IsScheduleActiveAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var status = await GetScheduleStatusAsync(scheduleId, cancellationToken);
        return status == ScheduleStatus.Enabled;
    }

    public async Task<DateTime?> GetNextExecutionTimeAsync(
      Guid scheduleId,
      CancellationToken cancellationToken = default)
    {
      try
      {
        var jobKeys = await GetJobKeysForScheduleAsync(
          scheduleId,
          cancellationToken);

        if (!jobKeys.Any())
          return null;

        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        return await FindNextExecutionTime(
          scheduler,
          jobKeys,
          cancellationToken);
      }
      catch (Exception exception)
      {
        Log.Error(
          exception,
          "Error getting next execution time for schedule {ScheduleId}",
          scheduleId);

        return null;
      }
    }
    private static async Task<DateTime?> FindNextExecutionTime(
      IScheduler scheduler,
      IEnumerable<string> jobKeys,
      CancellationToken cancellationToken)
    {
      DateTime? nextExecution = null;

      foreach (var jobKeyString in jobKeys)
      {
        var jobKey = new Quartz.JobKey(jobKeyString);
        var triggers = await scheduler.GetTriggersOfJob(
          jobKey,
          cancellationToken);

        UpdateNextExecutionFromTriggers(
          triggers,
          ref nextExecution);
      }

      return nextExecution;
    }
    private static void UpdateNextExecutionFromTriggers(
      IEnumerable<ITrigger> triggers,
      ref DateTime? nextExecution)
    {
      foreach (var trigger in triggers)
      {
        var nextFire = trigger.GetNextFireTimeUtc();
        if (!ShouldUpdateNextExecution(nextFire, nextExecution))
          continue;

        nextExecution = nextFire!.Value.DateTime;
      }
    }
    private static bool ShouldUpdateNextExecution(
      DateTimeOffset? nextFire,
      DateTime? nextExecution)
    {
      if (!nextFire.HasValue)
        return false;

      return !nextExecution.HasValue ||
             nextFire.Value.DateTime < nextExecution.Value;
    }


}
