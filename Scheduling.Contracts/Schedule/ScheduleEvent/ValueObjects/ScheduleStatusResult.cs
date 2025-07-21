using Scheduling.Contracts.Schedule.Enums;

namespace Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

public class ScheduleStatusResult
{
    public Guid ScheduleId { get; set; }
    public ScheduleStatus Status { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public DateTime? NextExecutionTime { get; set; }
}