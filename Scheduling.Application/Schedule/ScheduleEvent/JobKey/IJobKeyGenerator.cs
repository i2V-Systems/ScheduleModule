using Scheduling.Contracts;
using Scheduling.Contracts.AttachedResources.Enums;

namespace Application.Schedule.ScheduleEvent.JobKey;

// Job key generation interface
public interface IJobKeyGenerator
{
    string GenerateJobKey(Resources topic, ScheduleEventTrigger metadata);
    string GenerateTriggerKey(Resources topic, ScheduleEventTrigger metadata);
}