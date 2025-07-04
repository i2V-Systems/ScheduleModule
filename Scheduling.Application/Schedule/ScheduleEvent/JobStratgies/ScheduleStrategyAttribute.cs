using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule.ScheduleEvent.JobStratgies;

[AttributeUsage(AttributeTargets.Class)]
public class ScheduleStrategyAttribute: Attribute
{
    public ScheduleType ScheduleType { get; }
    public ScheduleSubType? SubType { get; }
    public int Priority { get; }
    public ScheduleStrategyAttribute(ScheduleType scheduleType)
    {
        ScheduleType = scheduleType;
    }
}