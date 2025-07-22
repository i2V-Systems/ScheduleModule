

using System.Reflection;
using System.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.Schedule;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Scheduling.Contracts.Extensions;

public static class ContractDependencyInjection
{
    public static IServiceCollection AddContractServices(this IServiceCollection services,IConfiguration configuration)
    { 
       // services.AddSingleton<IScheduleManager>();
       // services.AddSingleton<IResourceManager>();
       //
       //  // MediatR
       //  services.AddMediatR(cfg => 
       //      cfg.RegisterServicesFromAssembly(
       //          Assembly.Load(new AssemblyName("Scheduling.Contracts")))
       //      );

        return services;
    }
}