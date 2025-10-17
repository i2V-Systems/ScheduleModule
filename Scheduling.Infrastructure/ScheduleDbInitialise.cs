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
