using Coravel.Events.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts;

namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher;

public interface ITopicListener : IListener<ScheduleEventTrigger>
{
    string[] InterestedTopics { get; }
}

public class TopicAwareDispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private  List<ITopicListener> _topicSubscribers;

    public TopicAwareDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        RegisterTopicListeners();
    }
    private void RegisterTopicListeners()
    {
        Dictionary<string, List<ITopicListener>> _topicSubscriptions = new();
        var topicListeners = _serviceProvider.GetServices<ITopicListener>();
        foreach (var listener in topicListeners)
        {
            foreach (var topic in listener.InterestedTopics)
            {
                if (!_topicSubscriptions.ContainsKey(topic)) 
                    _topicSubscriptions[topic] = new List<ITopicListener>();
                _topicSubscriptions[topic].Add(listener);
            }
        }
        _topicSubscribers = _topicSubscriptions.Values
            .SelectMany(listeners => listeners)
            .Distinct()
            .ToList();
    }

    public  Task Broadcast<TEvent>(TEvent payload) where TEvent : IEvent
    {
        if (payload is ScheduleEventTrigger triggerEvent)
        {
            var tasks = 
                _topicSubscribers.Select(async subscriber =>
                { if(subscriber.InterestedTopics.Contains(triggerEvent.eventTopic))
                        await subscriber.HandleAsync(triggerEvent);
                });
        }
        return Task.CompletedTask;
    } 
}