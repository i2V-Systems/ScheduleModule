
using Application.Abstractions;
using Application.Schedule;
using Infrastructure.Schedule;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Scheduling.Contracts;
using Serilog;

namespace Infrastructure.Extensions;

public static class InfrastructureDependencyInjection
{

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        MapperConfigurationExpression config)
    {
        config.AddProfile(new MappingProfile());
        // Database Context
        services.AddDbContext<ScheduleDbContext>(options =>
        {
            options.UseNpgsql(
                    configuration.GetConnectionString("analytic"),
                    b =>
                    {
                        b.MigrationsAssembly("DataLayer");
                        b.UseVector();
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Scoped);



        services.AddSingleton<INotificationManager, NotificationManager>();
        // Register open generic - this works for any T
        services.AddTransient(typeof(IScheduleRepository<>), typeof(ScheduleRepository<>));
        
        // Register Job Execution Log Repository
        services.AddTransient<IJobExecutionLogRepository, JobExecutionLogRepository>();

        return services;
    }



}
