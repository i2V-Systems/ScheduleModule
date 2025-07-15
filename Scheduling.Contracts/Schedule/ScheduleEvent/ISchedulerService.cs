
using Scheduling.Contracts.AttachedResources.DTOs;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;
public interface  ISchedulerService
{ 
    // public  Task InitService();
    public  void UnscheduleJob(Guid scheduleId, IUnifiedScheduler scheduler); 
    // public   void ExecuteStartEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule);
    // public  void ExecuteEndEvent(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule,IUnifiedScheduler scheduler);
    public void ScheduleJob(ScheduleDto schedule, List<Resources> topics ,  IUnifiedScheduler scheduler);

}
