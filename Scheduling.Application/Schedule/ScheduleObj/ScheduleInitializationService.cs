// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Scheduling.Contracts.Schedule;
//
// namespace Application.Schedule.ScheduleObj;
//
// public class ScheduleInitializationService : IHostedService
// {
//     private readonly IServiceProvider _serviceProvider;
//     
//     public ScheduleInitializationService(IServiceProvider serviceProvider)
//     {
//         _serviceProvider = serviceProvider;
//     }
//     
//     public async Task StartAsync(CancellationToken cancellationToken)
//     {
//         // Get the singleton instance and initialize it
//         var scheduleManager = _serviceProvider.GetRequiredService<IScheduleManager>();
//         if (scheduleManager is ScheduleManager manager)
//         {
//             await manager.InitializeAsync();
//         }
//     }
//     
//     public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
// }