
using Application.Schedule;
using Infrastructure.Schedule;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Pgvector.EntityFrameworkCore;
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
                    })
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Scoped);



        // Register open generic - this works for any T
        services.AddTransient(typeof(IScheduleRepository<>), typeof(ScheduleRepository<>));

        return services;
    }



}
