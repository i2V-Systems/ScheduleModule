
using Application.Schedule.ScheduleEvent.JobStratgies.helper;
using Application.Schedule.ScheduleEvent.Scheduler;
using Application.Schedule.ScheduleEvent.SchedulerServices;
using Scheduling.Contracts;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;

namespace Application.Schedule.ScheduleEvent.JobStratgies;

internal interface IScheduleJobStrategy
{
        ScheduleTypeInfo SupportedType { get; }
        bool CanHandle(ScheduleType scheduleType);
        void ScheduleJob(Action<Guid, ScheduleEventType> taskToPerform, ScheduleDto schedule, IUnifiedScheduler scheduler, ISchedulerService eventExecutor); 
}