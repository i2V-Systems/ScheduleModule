using Common.Application.ModuleRegistration;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.ModuleRegistration;

public class SchedulePresentationRegistration : IPresentationRegistration
{
    public void RegisterControllers(IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(SchedulePresentationRegistration).Assembly)
            .AddControllersAsServices();
    }
}
