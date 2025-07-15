using Coravel.Events.Interfaces;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Scheduling.Contracts;

public class ScheduleEventTrigger : IEvent
{
    public DateTime triggeredAt { get; set; }
    public Guid scheduleId { get; set; }
    public ScheduleEventType eventType { get; set; }
    public ScheduleEventTrigger(Guid id,ScheduleEventType type)
    {
            triggeredAt = DateTime.UtcNow;
            scheduleId= id;
            eventType = type;
    }
}