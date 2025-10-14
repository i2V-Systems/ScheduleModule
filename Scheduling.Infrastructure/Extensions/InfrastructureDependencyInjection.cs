
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
    private static NpgsqlConnection connection;
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

        scheduleDbInitialise("scheduleScripts.sql", configuration);
        scheduleDbInitialise("quartz.sql", configuration);
      
        
        // Register open generic - this works for any T
        services.AddTransient(typeof(IScheduleRepository<>), typeof(ScheduleRepository<>));
     
        return services;
    }

    private static void scheduleDbInitialise(String scriptPath, IConfiguration configuration)
    {
        try
        {
            var path = "";
#if DEBUG
               path = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(),
                        "../ScheduleModule/Scheduling.Infrastructure",
                        "ScheduleScripts",
                        scriptPath  
                    );
#else
            path = System.IO.Path.Combine("./ScheduleScripts", scriptPath);
#endif
                      
            string script = File.ReadAllText(path);
            using (connection = new NpgsqlConnection(configuration.GetConnectionString("analytic")))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(script, connection))
                {
                    command.ExecuteNonQuery();
                    Log.Information("schedule db initialized successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in schedule Db Initialise:{0}", ex.Message);
        }
        finally
        {
            connection.Close();
        }
    }
    
}