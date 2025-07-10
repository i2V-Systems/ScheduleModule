using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Controllers;

namespace Presentation.Extensions;


public static class PresentationDependencyInjection
{
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var scheduleAssembly = Assembly.GetAssembly(typeof(SchedulingController)) ?? currentAssembly;

        services.AddControllers()
            .AddApplicationPart(scheduleAssembly)
            .AddControllersAsServices();
       
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(scheduleAssembly));
        return services;
    }
}
