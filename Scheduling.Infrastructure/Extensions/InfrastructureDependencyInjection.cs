using Application.Schedule;
using Infrastructure.Schedule;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<ScheduleDbContext>(options =>
        {
            options.UseNpgsql(
                    configuration.GetConnectionString("analytic"),
                    b => b.MigrationsAssembly("DataLayer"))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .EnableSensitiveDataLogging();
        }, ServiceLifetime.Transient);

        // Repositories
        services.AddScoped<IScheduleRepository, ScheduleRepository>();

        return services;
    }
}