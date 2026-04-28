namespace Domain.Entities;

public class JobExecutionLog
{
    public Guid Id { get; init; }
    public string JobName { get; init; }
    public string JobGroup { get; init; }
    public DateTime FiredAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public JobExecutionStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public long? DurationMs { get; init; }
}

public enum JobExecutionStatus
{
    Started,
    Completed,
    Failed
}
