// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Scheduling.Contracts.AttachedResources;
//
// namespace Application.AttachedResources;
//
// public class ResourceInitializationService : IHostedService
// {
//     private readonly IServiceProvider _serviceProvider;
//     
//     public ResourceInitializationService(IServiceProvider serviceProvider)
//     {
//         _serviceProvider = serviceProvider;
//     }
//     
//     public async Task StartAsync(CancellationToken cancellationToken)
//     {
//         // Get the singleton instance and initialize it
//         var resourceManager = _serviceProvider.GetRequiredService<IResourceManager>();
//         if (resourceManager is ResourceManager manager)
//         {
//             await manager.InitializeAsync();
//         }
//     }
//     
//     public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
// }