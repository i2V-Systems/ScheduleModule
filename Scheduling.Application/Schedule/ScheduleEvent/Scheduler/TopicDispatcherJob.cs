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

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var jobKey = context.JobDetail.Key;
            if (context.Recovering)
            {
                Log.Error("RECOVERING missed job execution for: {JobKey} at {RecoveryTime}", 
                    jobKey, DateTimeOffset.Now);
            }
            else
            {
                Log.Error("Normal job execution for: {JobKey} at {ExecutionTime}", 
                    jobKey, DateTimeOffset.Now);
            }
            
            var data = context.MergedJobDataMap;

            // Extract job data
            var scheduleId = Guid.Parse(data.GetString("scheduleId") ?? string.Empty);
            var eventTypeString = data.GetString("eventType") ?? string.Empty;
            var topicsJson = data.GetString("topics") ?? "[]";

            // Parse topics from JSON
            var topicStrings = JsonConvert.DeserializeObject<List<string>>(topicsJson) ?? new List<string>();

            // Convert string topics to enum values
            var topics = new List<Resources>();
            foreach (var topicString in topicStrings)
            {
                if (Enum.TryParse<Resources>(topicString, out var resource))
                {
                    topics.Add(resource);
                }
            }

            if (!Enum.TryParse<ScheduleEventType>(eventTypeString, out var eventType))
            {
                throw new InvalidOperationException($"Invalid event type: {eventTypeString}");
            }

            var eventTrigger = new ScheduleEventTrigger(scheduleId, eventType);
            var handlers = _serviceProvider.GetServices<ITopicAwareJobHandler>().ToList();
            var interestedHandlers = new List<ITopicAwareJobHandler>();

            foreach (var handler in handlers)
            {
                // Check if handler is interested in any of the topics
                var hasMatchingTopic = topics.Any(topic => handler.InterestedTopics.Contains(topic));

                if (hasMatchingTopic)
                {
                    interestedHandlers.Add(handler);
                }
            }

            // Process with interested handlers
            await ProcessWithHandlersAsync(eventTrigger, topics, interestedHandlers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job");
            throw;
        }
    }
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
            var topicHandlers = handlers.Where(h => h.InterestedTopics.Contains(topic)).ToList();
        
            foreach (var handler in topicHandlers)
            {
                try
                {
                    await handler.HandleAsync(eventTrigger, topic);
                    _logger.LogDebug("Handler {HandlerType} processed topic {Topic} successfully", 
                        handler.GetType().Name, topic);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {HandlerType} failed to process topic {Topic}", 
                        handler.GetType().Name, topic);
                }
            }
        }
    }

}
