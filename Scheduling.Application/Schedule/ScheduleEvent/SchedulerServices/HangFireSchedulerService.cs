using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Scheduler;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Serilog;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.SchedulerServices;

[ScopedService]
public class HangFireSchedulerService :ISchedulerService
{
    private  JobStrategyHelper _strategyFactory;
    private readonly IServiceProvider _serviceProvider;

    public HangFireSchedulerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _strategyFactory = _serviceProvider.GetService<JobStrategyHelper>();
    }

    public Task InitService()
    {
        return Task.CompletedTask;
    }

    public void UnscheduleJob(Guid scheduleId,IUnifiedScheduler scheduler)
    {
        try
        { 
            // Rec.TryUnschedule(scheduleId+nameof(jobIds._start));
            scheduler.Unschedule(scheduleId.ToString());
           
            Console.WriteLine($"Successfully unscheduled all jobs for schedule {scheduleId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unscheduling jobs for schedule {scheduleId}: {ex.Message}");
        } 
    }

    public void ExecuteStartEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule)
    {
        try {
            Log.Debug("{schedule.Id} executed for start at: ", DateTime.Now);
            taskToPerform(schedule.Id, ScheduleEventType.Start);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in start event for schedule {schedule.Id}", ex);
        }
    }

    public void ExecuteEndEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule,
        IUnifiedScheduler scheduler)
    {
        try {
            Log.Debug("{schedule.Id} executed for end at: ", DateTime.Now);
            taskToPerform(schedule.Id, ScheduleEventType.End);

            // Unschedule one-time schedules
            if (schedule.SubType == (ScheduleSubType?)ScheduleType.DateWise)
            {
                UnscheduleJob(schedule.Id,scheduler);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error in end event for schedule {schedule.Id}", ex);
        }
    }

    public void ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler)
    {
        try
        {
            var strategy = _strategyFactory.GetStrategy(schedule.Type);
            strategy.ScheduleJob(taskToPerform, schedule, scheduler, this);
        }
        catch (Exception ex)
        {
            Log.Error($"Error scheduling job for schedule {schedule.Id}", ex);
            throw;
        }
    }
}

