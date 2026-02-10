using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz;
using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.Scheduler;

[TransientService]
public class TopicDispatcherJob : IJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TopicDispatcherJob> _logger;

    public const string Name = nameof(TopicDispatcherJob);

    public TopicDispatcherJob(IServiceProvider serviceProvider, ILogger<TopicDispatcherJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    private static ScheduleEventTrigger CreateEventTrigger(
      JobExecutionData jobData)
    {
      return new ScheduleEventTrigger(
        jobData.ScheduleId,
        jobData.EventType);
    }

    public async Task Execute(IJobExecutionContext context)
    {
      try
      {
        LogExecution(context);

        var jobData = ExtractJobData(context.MergedJobDataMap);
        var topics = ParseTopics(jobData.TopicsJson);
        var eventTrigger = CreateEventTrigger(jobData);

        var handlers = GetInterestedHandlers(topics).ToList();

        await ProcessWithHandlersAsync(
          eventTrigger,
          topics,
          handlers);
      }
      catch (Exception exception)
      {
        _logger.LogError(exception, "Error processing job");
        throw;
      }
    }
    private static void LogExecution(IJobExecutionContext context)
    {
      var jobKey = context.JobDetail.Key;

      if (context.Recovering)
      {
        Log.Error(
          "RECOVERING missed job execution for: {JobKey} at {RecoveryTime}",
          jobKey,
          DateTimeOffset.Now);
        return;
      }

      Log.Error(
        "Normal job execution for: {JobKey} at {ExecutionTime}",
        jobKey,
        DateTimeOffset.Now);
    }
    private static JobExecutionData ExtractJobData(JobDataMap data)
    {
      var scheduleId = ParseScheduleId(data);
      var eventType = ParseEventType(data);

      return new JobExecutionData(
        scheduleId,
        eventType,
        data.GetString("topics") ?? "[]");
    }
    private static Guid ParseScheduleId(JobDataMap data)
    {
      var raw = data.GetString("scheduleId");

      if (!Guid.TryParse(raw, out var scheduleId))
        throw new InvalidOperationException(
          $"Invalid scheduleId: {raw}");

      return scheduleId;
    }

    private static ScheduleEventType ParseEventType(JobDataMap data)
    {
      var raw = data.GetString("eventType");

      if (!Enum.TryParse<ScheduleEventType>(raw, out var eventType))
        throw new InvalidOperationException(
          $"Invalid event type: {raw}");

      return eventType;
    }
    private static List<Resources> ParseTopics(string topicsJson)
    {
      var topicStrings =
        JsonConvert.DeserializeObject<List<string>>(topicsJson) ??
        new List<string>();

      var topics = new List<Resources>();

      foreach (var topic in topicStrings)
      {
        if (Enum.TryParse<Resources>(topic, out var resource))
        {
          topics.Add(resource);
        }
      }

      return topics;
    }
    private IReadOnlyList<ITopicAwareJobHandler> GetInterestedHandlers(
      IReadOnlyCollection<Resources> topics)
    {
      var handlers =
        _serviceProvider.GetServices<ITopicAwareJobHandler>();

      return handlers
        .Where(topicAwareJobHandler =>
          topics.Any(resources => topicAwareJobHandler.InterestedTopics.Contains(resources)))
        .ToList();
    }
    private sealed record JobExecutionData(
      Guid ScheduleId,
      ScheduleEventType EventType,
      string TopicsJson);

    private async Task ProcessWithHandlersAsync(
        ScheduleEventTrigger eventTrigger,
        List<Resources> topics,
        List<ITopicAwareJobHandler> handlers)
    {
        if (!handlers.Any())
        {
            _logger.LogWarning("No handlers found for topics: {Topics}", string.Join(", ", topics));
            return;
        }

        _logger.LogInformation("Processing {TopicCount} topics with {HandlerCount} handlers",
            topics.Count, handlers.Count);

        // Process each topic with all interested handlers
        foreach (var topic in topics)
        {
            var topicHandlers = handlers.Where(topicAwareJobHandler => topicAwareJobHandler.InterestedTopics.Contains(topic)).ToList();

            foreach (var handler in topicHandlers)
            {
                try
                {
                    await handler.HandleAsync(eventTrigger, topic);
                    _logger.LogDebug("Handler {HandlerType} processed topic {Topic} successfully",
                        handler.GetType().Name, topic);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Handler {HandlerType} failed to process topic {Topic}",
                        handler.GetType().Name, topic);
                }
            }
        }
    }

}
