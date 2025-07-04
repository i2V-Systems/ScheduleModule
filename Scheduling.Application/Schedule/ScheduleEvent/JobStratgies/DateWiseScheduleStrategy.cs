using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;


namespace Application.Schedule.ScheduleEvent.JobStratgies;

[ScopedService]
[ScheduleStrategy(ScheduleType.DateWise)]
internal class DateWiseScheduleStrategy : IScheduleJobStrategy
{
    public ScheduleTypeInfo SupportedType => new(ScheduleType.DateWise, name: "Date-wise Schedule", description: "Executes tasks on specific dates");

    public bool CanHandle(ScheduleType scheduleType)
    {
        return scheduleType == ScheduleType.DateWise;
    }

    public Task ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler, ISchedulerService eventExecutor)
    {
        var startTime = schedule.StartDateTime;
        var endTime = schedule.EndDateTime;

        scheduler.ScheduleDateWise(
            schedule.Id+ nameof(jobIds._date_start),
            () => eventExecutor.ExecuteStartEvent(taskToPerform, schedule),
            startTime.Hour,
            startTime.Minute,
            startTime.Date 
            );
        scheduler.ScheduleDateWise(
            schedule.Id+ nameof(jobIds._date_end),
            () => eventExecutor.ExecuteEndEvent(taskToPerform, schedule,scheduler),
            endTime.Hour,
            endTime.Minute,
            endTime.Date 
        );
        return Task.CompletedTask;
    }
}
    