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
        
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            // Add scheduler identity for clustering
            q.SchedulerId = "MyScheduler";
            q.SchedulerName = "MyQuartzScheduler";
            
            // Use persistent job store
            q.UsePersistentStore(s =>
            {
                s.RetryInterval = TimeSpan.FromSeconds(15);
                s.UsePostgres(cfg =>
                    {
                        cfg.ConnectionString = configuration.GetConnectionString("analytic");
                        cfg.TablePrefix = "scheduler.qrtz_";
                    },
                    dataSourceName: "schedulers");
                s.UseNewtonsoftJsonSerializer();
                s.UseClustering(c =>
                {
                    c.CheckinInterval = TimeSpan.FromSeconds(20);
                    c.CheckinMisfireThreshold = TimeSpan.FromSeconds(30);
                });
            });
            // Set misfire threshold
            q.MisfireThreshold=TimeSpan.FromSeconds(30);
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
