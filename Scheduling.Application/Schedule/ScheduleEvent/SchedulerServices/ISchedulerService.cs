
using Application.Schedule.ScheduleEvent.Scheduler;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;

namespace Application.Schedule.ScheduleEvent.SchedulerServices;
public interface  ISchedulerService
{ 
    public  Task InitService();
    public  void UnscheduleJob(Guid scheduleId, IUnifiedScheduler scheduler); 
    public   void ExecuteStartEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule);
    public  void ExecuteEndEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule,IUnifiedScheduler scheduler);
    public  Task ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler);

}
