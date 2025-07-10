using Coravel.Events.Interfaces;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;
public interface ITopicListener : IListener<ScheduleEventTrigger>
{
    string[] InterestedTopics { get; }
}