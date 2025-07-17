using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

namespace Scheduling.Contracts.Schedule.ScheduleEvent;

public interface IUnifiedScheduler
{
    Task<ScheduleResult> ScheduleDailyAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        TimeOnly time, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleSelectedDaysAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        TimeOnly time, 
        string cronExpression, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleWeekDaysAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        TimeOnly time, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleWeekendDaysAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        TimeOnly time, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleDateWiseAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        DateTime executeAt, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleMonthlyAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        int day, 
        TimeOnly time, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleCronAsync(
        IReadOnlyList<Resources> topics, 
        ScheduleEventTrigger metadata, 
        string cronExpression, 
        CancellationToken cancellationToken = default);
    Task<ScheduleResult> ScheduleOnceAsync(IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, DateTime executeAt, CancellationToken cancellationToken = default);
    Task<bool> UnscheduleAsync(string jobId, CancellationToken cancellationToken = default);
    Task<bool> UnscheduleAllAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);
}

