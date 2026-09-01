using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Listener;
using Scheduling.Contracts.AttachedResources;
using Scheduling.Contracts.Schedule;
using Serilog;

namespace Application.Schedule.ScheduleEvent.Scheduler;

public class ScheduleTriggerListener : TriggerListenerSupport
{
    private readonly IServiceProvider _serviceProvider;

    public override string Name => "ScheduleTriggerListener";

    public ScheduleTriggerListener(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public override async Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
    {
        Log.Warning("Misfire detected for trigger {TriggerKey}", trigger.Key);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduleManager = scope.ServiceProvider.GetService<IScheduleManager>();
            var entitiesManager = scope.ServiceProvider.GetService<IScheduledEntitiesManager>();
            var auditLogger = scope.ServiceProvider.GetService<IScheduleAuditLogger>();

            Guid scheduleId = Guid.Empty;
            if (trigger.JobDataMap.ContainsKey("scheduleId"))
            {
                var idStr = trigger.JobDataMap.GetString("scheduleId");
                Guid.TryParse(idStr, out scheduleId);
            }

            if (scheduleId == Guid.Empty)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    trigger.Key.Name,
                    @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}"
                );
                if (match.Success && Guid.TryParse(match.Value, out var parsed))
                {
                    scheduleId = parsed;
                }
            }

            var schedule = scheduleId != Guid.Empty ? scheduleManager?.GetScheduleFromCache(scheduleId) : null;
            string scheduleName = schedule?.Name;

            if (string.IsNullOrEmpty(scheduleName) && scheduleId != Guid.Empty)
            {
                try
                {
                    var repo = scope.ServiceProvider.GetService<Application.Schedule.IScheduleRepository<Domain.Schedule.Schedule>>();
                    var dbSchedule = repo?.Find(s => s.Id == scheduleId);
                    if (dbSchedule != null && !string.IsNullOrEmpty(dbSchedule.Name))
                    {
                        scheduleName = dbSchedule.Name;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(scheduleName))
            {
                scheduleName = trigger.Key.Name;
            }

            string userName = "i2vadmin";

            if (scheduleId != Guid.Empty && entitiesManager != null)
            {
                var resources = entitiesManager.GetResourcesByScheduleId(scheduleId);
                var firstWithMeta = resources?.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.metaData));
                if (firstWithMeta != null)
                {
                    try
                    {
                        var metaObj = JObject.Parse(firstWithMeta.metaData);
                        var createdBy = metaObj["CreatedBy"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(createdBy))
                        {
                            userName = createdBy;
                        }
                    }
                    catch { }
                }
            }

            string message = $"Schedule '{scheduleName}' missed its scheduled run time.";

            auditLogger?.LogActivity("Misfired", scheduleName, userName, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to log schedule misfire activity for trigger {TriggerKey}", trigger.Key);
        }

        await Task.CompletedTask;
    }
}
