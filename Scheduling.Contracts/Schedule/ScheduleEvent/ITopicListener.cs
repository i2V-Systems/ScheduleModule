using Coravel.Events.Interfaces;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;
public interface ITopicListener : IListener<ScheduleEventTrigger>
{
    List<Resources> InterestedTopics { get; }
}