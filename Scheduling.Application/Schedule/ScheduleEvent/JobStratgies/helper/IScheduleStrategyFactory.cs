using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule.ScheduleEvent.JobStratgies.helper;

// Strategy factory interface
public interface IScheduleStrategyFactory
{
    IScheduleJobStrategy GetStrategy(ScheduleType scheduleType);
    Dictionary<string, IScheduleJobStrategy> GetAllStrategies();
}