using Application.Schedule.ScheduleEvent.JobKey;
using Application.Schedule.ScheduleEvent.ScheduleDispatcher;
using Microsoft.Extensions.Logging;
using Quartz;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
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
            return await ScheduleJobsAsync(topics, metadata, trigger =>
                    trigger.WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnEveryDay()
                        .WithIntervalInHours(24)
                        .WithMisfireHandlingInstructionIgnoreMisfires()
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
            
            // Generate single job key for the entire schedule
            var jobKey = $"schedule-{metadata.scheduleId}-{metadata.eventType}-{Guid.NewGuid():N}";
            var triggerKey = $"trigger-{metadata.scheduleId}-{metadata.eventType}-{Guid.NewGuid():N}";
            
            var topicsJson = System.Text.Json.JsonSerializer.Serialize(topics.Select(t => t.ToString()).ToList());

            var jobData = new JobDataMap
            {
                { "scheduleId", metadata.scheduleId.ToString() },
                { "eventType", metadata.eventType.ToString() },
                { "topics" , topicsJson},
                { "originalTopicCount", topics.Count }
            };

            var job = JobBuilder.Create<TopicDispatcherJob>()
                .WithIdentity(jobKey)
                .RequestRecovery(true) // Enable recovery
                .StoreDurably(true)   // Keep job even if no triggers
                .SetJobData(jobData)
                .Build();
            
            // Configure the trigger
            var triggerBuilder = TriggerBuilder.Create().WithIdentity(triggerKey);
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
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnMondayThroughFriday()
                        .WithIntervalInHours(24)
                        .WithMisfireHandlingInstructionIgnoreMisfires())
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
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .WithDailyTimeIntervalSchedule(s => s
                        .StartingDailyAt(TimeOfDay.HourAndMinuteOfDay(time.Hour, time.Minute))
                        .OnSaturdayAndSunday()
                        .WithIntervalInHours(24)
                        .WithMisfireHandlingInstructionIgnoreMisfires())
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
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .WithCronSchedule(cronExpression, x=>
                            x.WithMisfireHandlingInstructionIgnoreMisfires())
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
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            var jobIds = new List<string>();

            foreach (var topic in topics)
            {
                var jobKey = _jobKeyGenerator.GenerateJobKey(topic, metadata);
                var triggerKey = _jobKeyGenerator.GenerateTriggerKey(topic, metadata);

                var job = CreateJob(jobKey, metadata);
                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .StartAt(executeAt)
                    .WithSimpleSchedule(x=>x
                        .WithRepeatCount(0)
                        .WithMisfireHandlingInstructionIgnoreMisfires())
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
            .WithIdentity(jobKey)
            .RequestRecovery(true) // Enable recovery
            .StoreDurably(true)   // Keep job even if no triggers
            .UsingJobData("scheduleId", metadata.scheduleId)
            .UsingJobData("EventType", metadata.eventType.ToString())
            .Build();
    }
}