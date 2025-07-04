using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;


namespace Application.Schedule.ScheduleEvent.JobStratgies;

[ScopedService]
[ScheduleStrategy(ScheduleType.Daily)]
internal class DailyScheduleStrategy : IScheduleJobStrategy
{
    public ScheduleTypeInfo SupportedType => new(ScheduleType.Daily, name: "Daily Schedule", description: "Executes tasks daily at specific times");


    public bool CanHandle(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.Daily;
    }

    public Task ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler, ISchedulerService eventExecutor)
    {
        if (schedule.SubType == ScheduleSubType.Every)
        {
            // TODO: Implement every N days logic
            ScheduleStartAndEndEventsEvery(taskToPerform, schedule, scheduler, eventExecutor);
            return Task.CompletedTask;
        }

        ScheduleStartAndEndEvents(taskToPerform, schedule, scheduler,eventExecutor);
        return Task.CompletedTask;
    }

    private void ScheduleStartAndEndEvents(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler,ISchedulerService eventExecutor)
    {
        var startTime = schedule.StartDateTime;
        var endTime = schedule.EndDateTime;
        
        scheduler.ScheduleDaily(
            schedule.Id+ nameof(jobIds._start),
            () => eventExecutor.ExecuteStartEvent(taskToPerform, schedule),
            startTime.Hour,
            startTime.Minute);
        
        scheduler.ScheduleDaily(
            schedule.Id+ nameof(jobIds._end),
            () => eventExecutor.ExecuteEndEvent(taskToPerform, schedule, scheduler),
            endTime.Hour,
            endTime.Minute);
    }
    private void ScheduleStartAndEndEventsEvery(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler, ISchedulerService eventExecutor)
    {
        // Specialized logic for "every N weeks"
        Console.WriteLine($"Executing weekly every N days schedule for {schedule.Id}");
    }
}
