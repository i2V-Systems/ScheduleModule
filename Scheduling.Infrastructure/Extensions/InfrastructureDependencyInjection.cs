
using Application.Schedule;
using Infrastructure.Schedule;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using CommonUtilityModule.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector.EntityFrameworkCore;
using Scheduling.Contracts;
using Serilog;

namespace Infrastructure.Extensions;

public static class InfrastructureDependencyInjection
{

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        MapperConfigurationExpression config)
    {
        config.AddProfile(new MappingProfile());
        // Database Context
        services.AddDbContext<ScheduleDbContext>((serviceProvider, options) =>
        {
            var connectionStrings = serviceProvider.GetRequiredService<IOptions<ConnectionStringOptions>>().Value;
            options.UseNpgsql(
                    connectionStrings.analytic,
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


        return services;
    }



}
