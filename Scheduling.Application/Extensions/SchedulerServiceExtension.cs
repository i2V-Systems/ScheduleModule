using Application.Schedule.ScheduleEvent.Scheduler;
using Coravel;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            "coravel" => services.AddCoravelScheduler(),
            "hangfire" => services.AddHangfireScheduler(configuration),
            _ => throw new InvalidOperationException($"Unsupported scheduler: {schedulingService}")
        };
    }

    private static IServiceCollection AddCoravelScheduler(this IServiceCollection services)
    {
        services.AddScheduler();
        services.AddEvents();
        services.AddSingleton<IUnifiedScheduler, CoravelUnifiedScheduler>();
        return services;
    }

    private static IServiceCollection AddHangfireScheduler(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(configuration.GetConnectionString("analytic")));
        services.AddHangfireServer();
        services.AddSingleton<IUnifiedScheduler, HangfireUnifiedScheduler>();
        return services;
    }
}
