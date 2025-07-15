using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;
using TanvirArjel.Extensions.Microsoft.DependencyInjection;

namespace Application.Schedule.ScheduleEvent.JobKey;

[TransientService]
public class JobKeyGenerator : IJobKeyGenerator
{
    public string GenerateJobKey(Resources topic, ScheduleEventTrigger metadata)
    {
        return $"job-{topic}-{metadata.scheduleId}-{metadata.eventType}-{Guid.NewGuid():N}";
    }

    public string GenerateTriggerKey(Resources topic, ScheduleEventTrigger metadata)
    {
        return $"trigger-{topic}-{metadata.scheduleId}-{metadata.eventType}-{Guid.NewGuid():N}";
    }
}