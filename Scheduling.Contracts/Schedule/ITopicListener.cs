namespace Scheduling.Contracts.Schedule;
public interface ITopicListener : IListener<ScheduleEventTrigger>
{
    string[] InterestedTopics { get; }
}