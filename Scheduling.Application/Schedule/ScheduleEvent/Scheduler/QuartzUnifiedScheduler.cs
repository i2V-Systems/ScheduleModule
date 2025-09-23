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
            
            return await ScheduleJobsAsync(topics, metadata, trigger =>
                    trigger.WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnEveryDay()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionDoNothing()
                       
                    )
                    , cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule daily jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule daily jobs", ex);
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
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var jobKey = $"schedule-{metadata.scheduleId}-{metadata.eventType}-{timestamp}";
            var triggerKey = $"trigger-{metadata.scheduleId}-{metadata.eventType}-{timestamp}";
            
            var topicsJson = System.Text.Json.JsonSerializer.Serialize(topics.Select(t => t.ToString()).ToList());

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

            return ScheduleResult.Success(new List<string> { jobKey });
            
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to schedule jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule jobs", ex);
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
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey, "DEFAULT")  // ← ADD GROUP HERE
                    .ForJob(jobKey, "DEFAULT")
                    .WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnMondayThroughFriday()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionDoNothing())
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception ex)
        {
           Log.Error(ex, "Failed to schedule weekday jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule weekday jobs", ex);
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
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey,"DEFAULT")
                    .ForJob(jobKey, "DEFAULT")
                    .WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnSaturdayAndSunday()
                        .WithIntervalInHours(24)
                        .InTimeZone(utcTimeZone)
                        .InTimeZone(utcTimeZone)
                        .WithMisfireHandlingInstructionDoNothing())
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to schedule weekend jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule weekend jobs", ex);
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
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey,"DEFAULT")
                    .ForJob(jobKey, "DEFAULT")
                    
                    .WithCronSchedule(cronExpression, x=>
                            x.InTimeZone(utcTimeZone)
                                .WithMisfireHandlingInstructionDoNothing())
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to schedule cron jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule cron jobs", ex);
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
                    .WithSimpleSchedule(x=>x
                        .WithRepeatCount(0)
                    )
                    .Build();

                await scheduler.ScheduleJob(job, trigger, cancellationToken);
                jobIds.Add(jobKey);
            }

            return ScheduleResult.Success(jobIds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to schedule one-time jobs for metadata {MetadataId}", metadata.scheduleId);
            return ScheduleResult.Failure("Failed to schedule one-time jobs", ex);
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to unschedule job {JobId}", jobId);
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to unschedule multiple jobs");
            return false;
        }
    }

    private static IJobDetail CreateJob(string jobKey, ScheduleEventTrigger metadata)
    {
        return JobBuilder.Create<TopicDispatcherJob>()
            .WithIdentity(jobKey, "DEFAULT")
            .RequestRecovery(true) // Enable recovery
            .StoreDurably(true)   // Keep job even if no triggers
            .UsingJobData("scheduleId", metadata.scheduleId)
            .UsingJobData("EventType", metadata.eventType.ToString())
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update schedule {ScheduleId}", scheduleId);
            return ScheduleResult.Failure("Failed to update schedule", ex);
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to pause jobs for schedule {ScheduleId}", scheduleId);
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
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to resume jobs for schedule {ScheduleId}", scheduleId);
            return false;
        }
    }
    public async Task<IReadOnlyList<string>> GetJobKeysForScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<Quartz.JobKey>.AnyGroup(), cancellationToken);
            
            var matchingJobKeys = new List<string>();
            
            foreach (var jobKey in jobKeys)
            {
                // Check if the job key contains the schedule ID
                if (jobKey.Name.Contains($"schedule-{scheduleId}"))
                {
                    matchingJobKeys.Add(jobKey.Name);
                }
                else
                {
                    // Alternative: Check job data for schedule ID
                    var jobDetail = await scheduler.GetJobDetail(jobKey, cancellationToken);
                    if (jobDetail?.JobDataMap.ContainsKey("scheduleId") == true)
                    {
                        var jobScheduleId = jobDetail.JobDataMap.GetString("scheduleId");
                        if (Guid.TryParse(jobScheduleId, out var parsedScheduleId) && parsedScheduleId == scheduleId)
                        {
                            matchingJobKeys.Add(jobKey.Name);
                        }
                    }
                }
            }
            
            return matchingJobKeys.AsReadOnly();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to get job keys for schedule {ScheduleId}", scheduleId);
            return Array.Empty<string>();
        }
    }
    public async Task<ScheduleStatus> GetScheduleStatusAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKeys = await GetJobKeysForScheduleAsync(scheduleId, cancellationToken);
            if (!jobKeys.Any())
            {
                return ScheduleStatus.NotFound;
            }
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var allPaused = true;
            var hasActiveTriggers = false;

            foreach (var jobKeyString in jobKeys)
            {
                var jobKey = new Quartz.JobKey(jobKeyString);
                var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);
            
                foreach (var trigger in triggers)
                {
                    var triggerState = await scheduler.GetTriggerState(trigger.Key, cancellationToken);
                
                    if (triggerState != TriggerState.Paused)
                    {
                        allPaused = false;
                    }
                
                    if (triggerState == TriggerState.Normal)
                    {
                        hasActiveTriggers = true;
                    }
                }
            }

            if (allPaused)
                return ScheduleStatus.Disabled;
            else if (hasActiveTriggers)
                return ScheduleStatus.Enabled;
            else
                return ScheduleStatus.Disabled;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting status for schedule {ScheduleId}", scheduleId);
            return ScheduleStatus.NotFound;
        }
    }
    public async Task<bool> IsScheduleActiveAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var status = await GetScheduleStatusAsync(scheduleId, cancellationToken);
        return status == ScheduleStatus.Enabled;
    }
    
    public async Task<DateTime?> GetNextExecutionTimeAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var jobKeys = await GetJobKeysForScheduleAsync(scheduleId, cancellationToken);
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            DateTime? nextExecution = null;
            foreach (var jobKeyString in jobKeys)
            {
                var jobKey = new Quartz.JobKey(jobKeyString);
                var triggers = await scheduler.GetTriggersOfJob(jobKey, cancellationToken);
                foreach (var trigger in triggers)
                {
                    var nextFire = trigger.GetNextFireTimeUtc();
                    if (nextFire.HasValue)
                    {
                        if (!nextExecution.HasValue || nextFire.Value.DateTime < nextExecution.Value)
                        {
                            nextExecution = nextFire.Value.DateTime;
                        }
                    }
                }
            }
            return nextExecution;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting next execution time for schedule {ScheduleId}", scheduleId);
            return null;
        }
    }

}