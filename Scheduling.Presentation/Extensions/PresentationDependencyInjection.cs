using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Extensions;


public static class PresentationDependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Add controllers from this assembly
        var scheduleAssembly = Assembly.Load("Scheduling.Presentation");
        services.AddMvc()
            .AddApplicationPart(scheduleAssembly)
            .AddControllersAsServices();

        // HTTP Context (if needed)
        services.AddHttpContextAccessor();

        return services;
    }
}
