using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace TanvirArjel.Extensions.Microsoft.DependencyInjection
{
    // Marker interfaces
    public interface ITransientService { }
    public interface IScopedService { }
    public interface ISingletonService { }

    // Attributes
    [AttributeUsage(AttributeTargets.Class)]
    public class TransientServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedServiceAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonServiceAttribute : Attribute { }

    public static class DependencyInjectionExtensions
    {
        private static void TryAddServiceDescriptor(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            if (!services.Any(s => s.ServiceType == serviceType && s.ImplementationType == implementationType))
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
        }

        public static IServiceCollection AddServicesOfType<T>(this IServiceCollection services)
        {
            var serviceType = typeof(T);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(s => {
                try { return s.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .Where(p => serviceType.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces().Where(i => i != serviceType && i != typeof(ITransientService) && i != typeof(IScopedService) && i != typeof(ISingletonService));
                var primaryInterface = interfaces.FirstOrDefault();
                
                ServiceLifetime lifetime = ServiceLifetime.Transient;
                if (typeof(IScopedService).IsAssignableFrom(serviceType)) lifetime = ServiceLifetime.Scoped;
                else if (typeof(ISingletonService).IsAssignableFrom(serviceType)) lifetime = ServiceLifetime.Singleton;

                if (primaryInterface != null)
                {
                    TryAddServiceDescriptor(services, primaryInterface, type, lifetime);
                }
                TryAddServiceDescriptor(services, type, type, lifetime);
            }

            return services;
        }

        public static IServiceCollection AddServicesWithAttributeOfType<T>(this IServiceCollection services) where T : Attribute
        {
            var attributeType = typeof(T);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(s => {
                try { return s.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .Where(p => p.GetCustomAttribute(attributeType) != null && p.IsClass && !p.IsAbstract);

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();
                var primaryInterface = interfaces.FirstOrDefault();

                ServiceLifetime lifetime = ServiceLifetime.Transient;
                if (attributeType == typeof(ScopedServiceAttribute)) lifetime = ServiceLifetime.Scoped;
                else if (attributeType == typeof(SingletonServiceAttribute)) lifetime = ServiceLifetime.Singleton;

                if (primaryInterface != null)
                {
                    TryAddServiceDescriptor(services, primaryInterface, type, lifetime);
                }
                TryAddServiceDescriptor(services, type, type, lifetime);
            }

            return services;
        }

        public static IServiceCollection AddServicesByAttribute<T>(this IServiceCollection services, params string[] assemblyNames) where T : Attribute
        {
            var attributeType = typeof(T);
            foreach (var name in assemblyNames)
            {
                try
                {
                    var assembly = Assembly.Load(name);
                    var types = assembly.GetTypes()
                        .Where(p => p.GetCustomAttribute(attributeType) != null && p.IsClass && !p.IsAbstract);

                    foreach (var type in types)
                    {
                        var interfaces = type.GetInterfaces();
                        var primaryInterface = interfaces.FirstOrDefault();

                        ServiceLifetime lifetime = ServiceLifetime.Transient;
                        if (attributeType.Name.Contains("Scoped")) lifetime = ServiceLifetime.Scoped;
                        else if (attributeType.Name.Contains("Singleton")) lifetime = ServiceLifetime.Singleton;

                        if (primaryInterface != null)
                        {
                            TryAddServiceDescriptor(services, primaryInterface, type, lifetime);
                        }
                        TryAddServiceDescriptor(services, type, type, lifetime);
                    }
                }
                catch
                {
                    // Ignore loading errors
                }
            }
            return services;
        }
    }
}

namespace CommonUtilityModule.Models
{
    public interface IEntityBase
    {
        Guid Id { get; set; }
    }
}

namespace CommonUtilityModule.CrudUtilities
{
    public enum CrudMethodType
    {
        Add,
        Update,
        Delete,
        ScheduleAttachmentChanged
    }
}

namespace CommonUtilityModule.Manager
{
    public enum CrudRelatedEntity
    {
        Schedule
    }

    public static class CrudManager
    {
        public static System.Threading.Tasks.Task SendCrudDataToClient(
            CrudRelatedEntity entity,
            CommonUtilityModule.CrudUtilities.CrudMethodType method,
            Dictionary<string, dynamic> resources,
            List<string> skipUserIds = null,
            List<string> targetUserIds = null)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
