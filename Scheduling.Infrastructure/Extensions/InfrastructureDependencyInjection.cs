
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
      MappingProfile mappingProfile = new MappingProfile();
        config.AddProfile(mappingProfile);
        // Database Context
        services.AddDbContext<ScheduleDbContext>(options =>
        {
          string? ConnectionString = configuration.GetConnectionString("analytic");
            options.UseNpgsql(
                ConnectionString,
                    npgsqlDbContextOptionsBuilder =>
                    {
                        npgsqlDbContextOptionsBuilder.MigrationsAssembly("DataLayer");
                        npgsqlDbContextOptionsBuilder.UseVector();
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
