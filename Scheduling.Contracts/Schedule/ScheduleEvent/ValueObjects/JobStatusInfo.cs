namespace Scheduling.Contracts.Schedule.ScheduleEvent.ValueObjects;

public class JobStatusInfo
{
    public string JobKey { get; set; } = string.Empty;
    public string TriggerKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Paused, Complete, Error, Blocked
    public DateTime? NextFireTime { get; set; }
    public DateTime? PreviousFireTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ExecutionCount { get; set; }
    public string EventType { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsRecoverable { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
}