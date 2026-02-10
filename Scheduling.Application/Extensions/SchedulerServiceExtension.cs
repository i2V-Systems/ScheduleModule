using System.Diagnostics;
using System.Text.Json;
using Application.Schedule.ScheduleEvent.Scheduler;
using Coravel;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Scheduling.Contracts.Schedule.ScheduleEvent;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Application.Extensions;

public static class SchedulerServiceExtensions
{
    public static IServiceCollection AddSchedulingScheduler(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var schedulingService = configuration.GetValue<string>("SchedulingService") ??
            throw new InvalidOperationException("SchedulingService configuration is missing");

        return schedulingService.ToLowerInvariant() switch
        {
            // "coravel" => services.AddCoravelScheduler(),
            // "hangfire" => services.AddHangfireScheduler(configuration),
            "quartz" => services.AaddQuartzScheduler(configuration),
            _ => throw new InvalidOperationException($"Unsupported scheduler: {schedulingService}")
        };
    }
    private static IServiceCollection AaddQuartzScheduler(this IServiceCollection services,IConfiguration configuration)
    {

        services.AddQuartz(serviceCollectionQuartzConfigurator =>
        {
            serviceCollectionQuartzConfigurator.UseMicrosoftDependencyInjectionJobFactory();
            // Add scheduler identity for clustering
            serviceCollectionQuartzConfigurator.SchedulerId = "MyScheduler";
            serviceCollectionQuartzConfigurator.SchedulerName = "MyQuartzScheduler";
            var connectionString = configuration.GetConnectionString("analytic");

            // Use persistent job store
            serviceCollectionQuartzConfigurator.UsePersistentStore(persistentStoreOptions =>
            {
                persistentStoreOptions.RetryInterval = TimeSpan.FromSeconds(15);

                persistentStoreOptions.UsePostgres(cfg =>
                    {
                      cfg.ConnectionString = connectionString ?? throw new InvalidOperationException("ConnectionString is null");
                        cfg.TablePrefix = "scheduler.qrtz_";
                    },
                    dataSourceName: "schedulers");
                persistentStoreOptions.UseNewtonsoftJsonSerializer();
                persistentStoreOptions.PerformSchemaValidation = false;
                // s.UseClustering(c =>
                // {
                //     c.CheckinInterval = TimeSpan.FromSeconds(20);
                //     c.CheckinMisfireThreshold = TimeSpan.FromSeconds(30);
                // });
            });
            // Set misfire threshold
            serviceCollectionQuartzConfigurator.MisfireThreshold=TimeSpan.FromSeconds(30);
        });
        // Add Quartz.NET as a hosted service
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        services.AddSingleton<IUnifiedScheduler, QuartzUnifiedScheduler>();
        return services;
    }
}
