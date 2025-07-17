// using Application.Schedule.ScheduleEvent.JobStratgies.helper;
// using Microsoft.Extensions.Logging;
// using Scheduling.Contracts;
// using Scheduling.Contracts.AttachedResources.Enums;
// using Scheduling.Contracts.Schedule.DTOs;
// using Scheduling.Contracts.Schedule.Enums;
// using Scheduling.Contracts.Schedule.ScheduleEvent;
// using Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;
// using TanvirArjel.Extensions.Microsoft.DependencyInjection;
//
// namespace Application.Schedule.ScheduleEvent.JobStratgies;
// [TransientService]
// [ScheduleStrategy(ScheduleType.Monthly)]
// internal class MonthlyScheduleStrategy : IScheduleJobStrategy
// {
//     private readonly ILogger<MonthlyScheduleStrategy> _logger;
//
//     public MonthlyScheduleStrategy(ILogger<MonthlyScheduleStrategy> logger)
//     {
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     }
//
//     public ScheduleTypeInfo SupportedType => new(ScheduleType.Monthly, name: "Monthly Schedule", description: "Executes tasks monthly on specific days");
//
//     public bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Monthly;
//
//     public async Task<ScheduleResult> ScheduleJobAsync(ScheduleDto schedule, IReadOnlyList<Resources> topics, IUnifiedScheduler scheduler, CancellationToken cancellationToken = default)
//     {
//         try
//         {
//             var allJobIds = new List<string>();
//
//             var startTime = TimeOnly.FromDateTime(schedule.StartDateTime);
//             var dayOfMonth = schedule.StartDateTime.Day;
//             
//             if (schedule.EndDateTime != null)
//             {
//                 // Schedule start event
//                 var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Start);
//                 var startResult = await scheduler.ScheduleMonthlyAsync(topics, startTrigger, dayOfMonth, startTime, cancellationToken);
//             
//                 if (!startResult.IsSuccess)
//                     return startResult;
//
//                 allJobIds.AddRange(startResult.ScheduledJobIds);
//
//                 var endTime = TimeOnly.FromDateTime(schedule.EndDateTime?? DateTime.Now);
//                 // Schedule end event if different from start
//                 if (startTime != endTime)
//                 {
//                     var endTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.End);
//                     var endResult = await scheduler.ScheduleMonthlyAsync(topics, endTrigger, dayOfMonth, endTime, cancellationToken);
//                 
//                     if (!endResult.IsSuccess)
//                         return endResult;
//
//                     allJobIds.AddRange(endResult.ScheduledJobIds);
//                 }
//
//                 _logger.LogInformation("Successfully scheduled monthly jobs for schedule {ScheduleId} on day {DayOfMonth}", schedule.Id, dayOfMonth);
//             }
//             else
//             {
//                 
//
//                 // Schedule start event
//                 var startTrigger = new ScheduleEventTrigger(schedule.Id, ScheduleEventType.Once);
//                 var startResult = await scheduler.ScheduleMonthlyAsync(topics, startTrigger, dayOfMonth, startTime, cancellationToken);
//             
//                 if (!startResult.IsSuccess)
//                     return startResult;
//
//                 allJobIds.AddRange(startResult.ScheduledJobIds);
//             }
//            return ScheduleResult.Success(allJobIds);
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error in monthly schedule strategy for schedule {ScheduleId}", schedule.Id);
//             return ScheduleResult.Failure("Error in monthly schedule strategy", ex);
//         }
//     }
// }