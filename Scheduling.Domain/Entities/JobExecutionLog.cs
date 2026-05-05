namespace Domain.Entities;

public class JobExecutionLog
{
    public Guid Id { get; init; }
    public string JobName { get; init; }
    public string JobGroup { get; init; }
    public DateTime FiredAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public JobExecutionStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public long? DurationMs { get; set; }

    public string TriggerName { get; set; } = null!;
    public string TriggerGroup { get; set; } = null!;
    public string? TriggerDescription { get; set; }
    public DateTime? ScheduledFireTime { get; set; }   // when it was supposed to fire
    public DateTime? NextFireTime { get; set; }         // when it will fire next
}

public enum JobExecutionStatus
{
    Started,
    Completed,
    Failed
}
