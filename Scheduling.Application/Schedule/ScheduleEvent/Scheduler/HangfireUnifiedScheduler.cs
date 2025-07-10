
using Hangfire;
using Scheduling.Contracts.Schedule.ScheduleEvent;


namespace Application.Schedule.ScheduleEvent.Scheduler;

public class HangfireUnifiedScheduler: IUnifiedScheduler
{
    private readonly IRecurringJobManager _recurringJobManager;

    public HangfireUnifiedScheduler(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }
    public void ScheduleDaily(string jobId, Action action, int hour, int minute)
    {
        _recurringJobManager.AddOrUpdate(jobId, ()=>action(), Cron.Daily(hour,minute));
    }

    public void ScheduleSelectedDays(string jobId, Action action, int hour, int minute,string cronn)
    {
        RecurringJob.AddOrUpdate(jobId, ()=> action(), cronn);
    }

    public void ScheduleWeekDays(string jobId, Action action, int hour, int minute)
    {
        var cronExpression = $"{minute} {hour} * * 1-5";
        RecurringJob.AddOrUpdate(jobId, ()=> action(), cronExpression);
    }
    public void ScheduleWeekendDays(string jobId, Action action, int hour, int minute)
    { 
        var cronExpression = $"{minute} {hour} * * 6,0";
        RecurringJob.AddOrUpdate(jobId, ()=> action(), cronExpression);
    }
    public void ScheduleDateWise(string jobId, Action action,int hour, int minute,DateTime date)
    {
        var scheduleTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        BackgroundJob.Schedule(() => action(), scheduleTime);
    }
    public void ScheduleMonthly(string jobId, Action action, int day, int hour, int minute)
    {
        var cronExpression = $"{minute} {hour} {day} * *";
        _recurringJobManager.AddOrUpdate(jobId, () => action(), cronExpression);
    }

    public void ScheduleCron(string jobId, Action action, string cronExpression)
    {
        _recurringJobManager.AddOrUpdate(jobId, () => action(), cronExpression);
    }

    public void ScheduleOnce(string jobId, Action action, DateTime executeAt)
    {
        BackgroundJob.Schedule(() => action(), executeAt);
    }

    public void Unschedule(string jobId)
    {
        try
        {
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._start));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._end));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._date_start));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._date_end));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._weekday_start));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._weekday_end));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._weekend_end));
            _recurringJobManager.RemoveIfExists(jobId+nameof(jobIds._weekend_start));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error unscheduling Hangfire job {jobId}: {ex.Message}");
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