using Quartz;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;

public interface ITopicAwareJobHandler
{
    IReadOnlyList<Resources> InterestedTopics { get; }
    Task HandleAsync(ScheduleEventTrigger eventTrigger, Resources topics);
}