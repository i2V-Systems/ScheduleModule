using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;

namespace Infrastructure;

public static class ScheduleDbInitialise
{
  private static NpgsqlConnection connection;
  public  static void scheduleDbInitialise(String scriptPath, IConfiguration configuration)
  {
    try
    {
      var path = "";
#if DEBUG
      var currentDirectory = Directory.GetCurrentDirectory();
      path = System.IO.Path.Combine(
        currentDirectory,
        "../ScheduleModule/Scheduling.Infrastructure",
        "ScheduleScripts",
        scriptPath
      );
#else
            path = System.IO.Path.Combine("./ScheduleScripts", scriptPath);
#endif

      string script = File.ReadAllText(path);
      string? connectionString = configuration.GetConnectionString("analytic");
      using (connection = new NpgsqlConnection(connectionString))
      {
        connection.Open();
        using (var command = new NpgsqlCommand(script, connection))
        {
          command.ExecuteNonQuery();
          Log.Information("schedule db initialized successfully.");
        }
      }
    }
    catch (Exception exception)
    {
      Log.Error("Error in schedule Db Initialise:{0}", exception.Message);
    }
    finally
    {
      connection.Close();
    }
  }

}
