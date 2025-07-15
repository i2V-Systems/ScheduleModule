using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

namespace Application.Schedule.ScheduleEvent.JobStratgies.helper;

public interface IScheduleJobStrategy
{
        Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default);
        bool CanHandle(ScheduleType scheduleType);
}