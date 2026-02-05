using System.Reflection;
using Application.Extensions;
using AutoMapper;
using Infrastructure;
using Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Extensions;
using Scheduling.Contracts.Extensions;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Presentation;

public enum SchedulerType
{
    Coravel,
    Hangfire
}

public static class ScheduleModuleRegistration
{
    private static IServiceProvider ServiceProvider;
    public static SchedulerType CurrentSchedulerType { get; private set; }

    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services)
    {
        return services;
    }

    public static async Task InitializeManager(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        await ApplicationDependencyInjection.InitialiseManagers(serviceProvider);
    }

  public static void StartSeedingData(IConfiguration configuration)
  {
    ScheduleDbInitialise.scheduleDbInitialise("scheduleScripts.sql", configuration);
    ScheduleDbInitialise.scheduleDbInitialise("quartz.sql", configuration);

  }

    public static void ConfigureSchedulingServices(this IServiceCollection services, IConfiguration configuration,MapperConfigurationExpression config)
    {
        services.AddContractServices(configuration);
        services.AddInfrastructureServices(configuration,config);
        services.AddApplicationServices(configuration);

        var scheduleAssembly = Assembly.Load("Scheduling.Presentation");
        var businessAssembly = Assembly.Load("BusinessLayer");

        // Register services from both assemblies with interface support
        // Use the distinct method names to avoid ambiguity
        services.AddServicesByAttribute<TransientServiceAttribute>(
            "Scheduling.Presentation",
            "BusinessLayer");

        // Also register other service types
        //services.AddServicesByAttribute<ScopedServiceAttribute>(
        //    "Scheduling.Presentation",
        //    "BusinessLayer");

        //services.AddServicesByAttribute<SingletonServiceAttribute>(
        //    "Scheduling.Presentation",
        //    "BusinessLayer");


        services.AddControllers()
            .AddApplicationPart(scheduleAssembly)
            .AddControllersAsServices();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(scheduleAssembly)
        );
    }



}

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddServicesByAttribute<TAttribute>(
        this IServiceCollection services,
        params string[] assemblyNames)
        where TAttribute : Attribute
    {
        ServiceLifetime lifetime = GetLifetimeFromAttribute<TAttribute>();

        var assemblies = assemblyNames
            .Select(Assembly.Load)
            .ToArray();

        var types = assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsDefined(typeof(TAttribute), false))
            .Where(type => type.IsClass && !type.IsAbstract);

        foreach (var type in types)
        {
            // Register only relevant interfaces, not ALL interfaces
            RegisterRelevantInterfaces(services, type, lifetime);
        }

        return services;
    }

    private static void RegisterRelevantInterfaces(IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        var interfaces = type.GetInterfaces();

        foreach (var interfaceType in interfaces)
        {
            // Skip system interfaces and other non-relevant interfaces
            if (ShouldRegisterInterface(interfaceType))
            {
              var serviceDescriptor = new ServiceDescriptor(interfaceType, type, lifetime);
                services.Add(serviceDescriptor);
            }
        }
    }

    private static bool ShouldRegisterInterface(Type interfaceType)
    {
        // Skip system interfaces
        if (interfaceType.Namespace?.StartsWith("System",StringComparison.CurrentCulture) == true)
            return false;

        if (interfaceType.Namespace?.StartsWith("Microsoft",StringComparison.CurrentCulture) == true)
            return false;

        if (interfaceType.Namespace?.StartsWith("Windows",StringComparison.CurrentCulture) == true)
            return false;

        // Skip disposable interface (it's handled by the framework)
        if (interfaceType == typeof(IDisposable) || interfaceType == typeof(IAsyncDisposable))
            return false;

        // Skip marker interfaces or other non-service interfaces
        if (interfaceType.Name.StartsWith("IEnumerable",StringComparison.CurrentCulture) ||
            interfaceType.Name.StartsWith("ICollection",StringComparison.CurrentCulture) ||
            interfaceType.Name.StartsWith("IList",StringComparison.CurrentCulture))
            return false;

        return true;
    }

    private static ServiceLifetime GetLifetimeFromAttribute<TAttribute>()
        where TAttribute : Attribute
    {
        string attributeName = typeof(TAttribute).Name;

        if (attributeName.Contains("Transient", StringComparison.OrdinalIgnoreCase))
            return ServiceLifetime.Transient;
        if (attributeName.Contains("Scoped", StringComparison.OrdinalIgnoreCase))
            return ServiceLifetime.Scoped;
        if (attributeName.Contains("Singleton", StringComparison.OrdinalIgnoreCase))
            return ServiceLifetime.Singleton;

        return ServiceLifetime.Transient;
    }
}
