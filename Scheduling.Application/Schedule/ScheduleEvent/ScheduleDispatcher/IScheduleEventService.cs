using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.DTOs;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

namespace Application.Schedule.ScheduleEvent.ScheduleDispatcher;

// Main scheduling service interface
public interface IScheduleEventService
{
    Task<ScheduleResult> ExecuteAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, CancellationToken cancellationToken = default);
    Task<ScheduleResult> UpdateAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, CancellationToken cancellationToken = default);
    Task<ScheduleResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}