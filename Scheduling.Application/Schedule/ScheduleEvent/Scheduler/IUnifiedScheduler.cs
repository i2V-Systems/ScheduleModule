using Coravel;

namespace Application.Schedule.ScheduleEvent.Scheduler;

public interface IUnifiedScheduler
{
    void ScheduleDaily(string jobId, Action action, int hour, int minute);
    void ScheduleSelectedDays(string jobId, Action action,int hour, int minute, string cronn);
    void ScheduleWeekDays(string jobId, Action action,int hour, int minute);
    void ScheduleWeekendDays(string jobId, Action action,int hour, int minute);
    void ScheduleDateWise(string jobId, Action action, int hour, int minute,DateTime date);
    
    void ScheduleMonthly(string jobId, Action action, int day, int hour, int minute);
    void ScheduleCron(string jobId, Action action, string cronExpression);
    void ScheduleOnce(string jobId, Action action, DateTime executeAt);
    void Unschedule(string jobId);
    void UnscheduleAll(params string[] jobIds);
}

public enum jobIds
{
  _start,
  _end,
  _weekday_start,
  _weekday_end,
  _weekend_start,
  _weekend_end,
  _date_start,
  _date_end
  
}

