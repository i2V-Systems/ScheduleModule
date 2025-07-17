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
//
// [TransientService]
// [ScheduleStrategy(ScheduleType.Yearly)]
// internal class YearlyScheduleStrategy : BaseScheduleStrategy
// {
//
//     public YearlyScheduleStrategy(ILogger<YearlyScheduleStrategy> logger):base(logger){}
//
//     public override ScheduleTypeInfo SupportedType => new(
//         ScheduleType.Yearly, 
//         name: "Yearly Schedule", 
//         description: "Executes tasks annually on specific dates");
//
//     public override bool CanHandle(ScheduleType scheduleType) => scheduleType == ScheduleType.Yearly;
//
//     protected override async Task<ScheduleResult> ExecuteStrategyAsync(
//         ScheduleDto schedule, 
//         IReadOnlyList<Resources> topics, 
//         IUnifiedScheduler scheduler, 
//         CancellationToken cancellationToken = default)
//     {
//         return await ScheduleEvents( 
//             schedule, 
//             topics, 
//             scheduler,
//             scheduler.ScheduleYearlyAsync,
//             cancellationToken
//         );
//         
//     }
// }