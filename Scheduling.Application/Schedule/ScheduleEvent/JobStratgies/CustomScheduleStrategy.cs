using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;


namespace Application.Schedule.ScheduleEvent.JobStratgies;
// Example of how to add a new strategy in the future
[ScopedService]
[ScheduleStrategy(ScheduleType.Custom)]
internal class CustomScheduleStrategy : IScheduleJobStrategy
{
    public ScheduleTypeInfo SupportedType => new(ScheduleType.Custom, name: "Custom Schedule", description: "Custom scheduling logic");
    public bool CanHandle(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.Custom;
    }

    public void ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler, ISchedulerService eventExecutor)
    {
        // Custom scheduling logic here
        Console.WriteLine($"Executing custom schedule for {schedule.Id}");
    }
}
