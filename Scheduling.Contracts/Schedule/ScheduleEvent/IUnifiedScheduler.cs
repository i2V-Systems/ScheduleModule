using Quartz;
using Scheduling.Contracts.AttachedResources.Enums;
using Scheduling.Contracts.Schedule.Enums;
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
    
    // Management operations
    Task<bool> UnscheduleAsync(string jobId, CancellationToken cancellationToken = default);
    Task<bool> UnscheduleAllAsync(IEnumerable<string> jobIds, CancellationToken cancellationToken = default);
    
    // operations for update, enable/disable
    Task<ScheduleResult> UpdateScheduleAsync(Guid scheduleId, IReadOnlyList<Resources> topics, ScheduleEventTrigger metadata, Func<TriggerBuilder, TriggerBuilder> configureTrigger, CancellationToken cancellationToken = default);
    Task<bool> PauseJobAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<bool> ResumeJobAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetJobKeysForScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    Task<ScheduleStatus> GetScheduleStatusAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<bool> IsScheduleActiveAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<DateTime?> GetNextExecutionTimeAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    
}

