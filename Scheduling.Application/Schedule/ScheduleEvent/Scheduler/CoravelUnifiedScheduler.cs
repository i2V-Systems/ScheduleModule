
using Coravel.Scheduling.Schedule.Interfaces;
using Scheduling.Contracts.Schedule.ScheduleEvent;


namespace Application.Schedule.ScheduleEvent.Scheduler;

public class CoravelUnifiedScheduler : IUnifiedScheduler
{
    private readonly IScheduler _scheduler;

    public CoravelUnifiedScheduler(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public void ScheduleDaily(string jobId, Action action, int hour, int minute)
    {
        _scheduler.Schedule(action)
            .DailyAt(hour, minute)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(jobId);
    }

    public void ScheduleSelectedDays(string jobId, Action action,int hour, int minute,string cronn)
    {
        _scheduler.Schedule(action)
            .Cron(cronn)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(jobId);
    }
    public void ScheduleWeekDays(string jobId, Action action,int hour, int minute)
    {
        _scheduler.Schedule(action)
            .DailyAt(hour, minute)
            .Weekday()
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(jobId);
    }
    public void ScheduleWeekendDays(string jobId, Action action,int hour, int minute)
    {
        _scheduler.Schedule(action)
            .DailyAt(hour, minute)
            .Weekend()
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(jobId);
    }

    public void ScheduleDateWise(string jobId, Action action,int hour, int minute,DateTime date)
    {
        _scheduler.Schedule(action)
            .DailyAt(hour, minute)
            .Zoned(TimeZoneInfo.Local)
            .PreventOverlapping(jobId)
            .When(() => Task.FromResult(date == DateTime.Now.Date));
    }

    public void ScheduleMonthly(string jobId, Action action, int day, int hour, int minute)
    {
       
    }

    public void ScheduleCron(string jobId, Action action, string cronExpression)
    {
        _scheduler.Schedule(action)
            .Cron(cronExpression)
            .PreventOverlapping(jobId);
    }

    public void ScheduleOnce(string jobId, Action action, DateTime executeAt)
    {
        // Coravel doesn't support one-time scheduling, so we use a workaround
        var delay = executeAt - DateTime.Now;
        if (delay > TimeSpan.Zero)
        {
            Task.Run(async () =>
            {
                await Task.Delay(delay);
                action();
            });
        }
    }

    public void Unschedule(string scheduleId)
    {
        try
        {
            if (_scheduler is Coravel.Scheduling.Schedule.Scheduler coravelScheduler)
            {
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._start));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._end));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._date_start));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._date_end));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._weekday_start));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._weekday_end));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._weekend_start));
                coravelScheduler.TryUnschedule(scheduleId+nameof(jobIds._weekend_end));
                Console.WriteLine($"Successfully unscheduled all jobs for schedule {scheduleId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unscheduling jobs for schedule {scheduleId}: {ex.Message}");
        }
    }

    public void UnscheduleAll(params string[] jobIds)
    {
        foreach (var jobId in jobIds)
        {
            Unschedule(jobId);
        }
    }
}